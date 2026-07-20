-- =====================================================================
-- AyNesil Platform :: Layer 2 — Care Team Assignment
-- Effective-dated M:N educator↔student care-team membership.
-- ABAC Phase 2 DDL — implements ABAC_CARE_TEAM_DESIGN.md §2.2 & §7.
--
-- Triggers (trg_set_updated_at, trg_audit) and the tenant-isolation
-- RLS policy are applied automatically by db/99_triggers_rls_policies.sql,
-- which runs after this file in smoke_test.sh.
-- Flyway migration V15 applies those same objects explicitly.
--
-- Ref categories used (seeded in 01_reference_data_seed.sql / V15):
--   role_id       → ref_type 'care_team_role'
--   grant_type_id → ref_type 'care_team_grant_type'
-- =====================================================================

create table students.student_care_assignment (
  id                   uuid primary key default core.uuid_generate_v7(),
  corporation_id       uuid not null references core.corporation(id),
  student_id           uuid not null references students.student(id),
  educator_id          uuid not null references educators.educator(id),
  campus_id            uuid references core.campus(id),               -- nullable; scope of this assignment
  role_id              uuid not null references ref.ref_value(id),    -- ref_type 'care_team_role'
  is_primary           boolean not null default false,
  status               text not null default 'active'
                         check (status in ('active', 'suspended', 'ended')),
  active_from          date not null default current_date,
  active_to            date,                                          -- NULL = open-ended
  -- Forward-looking delegation / substitution provenance columns (design §7)
  grant_type_id        uuid references ref.ref_value(id),            -- ref_type 'care_team_grant_type'; NULL = permanent
  source_assignment_id uuid references students.student_care_assignment(id), -- original assignment being covered
  granted_by           uuid references iam.user_account(id),         -- user who created this grant
  reason               text,                                          -- required for emergency / delegated types
  -- Standard audit columns
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1,
  constraint chk_care_assignment_dates check (active_to is null or active_to >= active_from)
);

comment on table students.student_care_assignment
  is 'Durable, effective-dated care-team edge: educator↔student with configurable role, '
     'campus scope, and delegation/substitution provenance (ABAC Phase 2). '
     'Phase 3 attaches a RESTRICTIVE RLS policy via students.user_can_access_student(). '
     'History is preserved (soft delete only) so the audit trail of who could see what, when, survives.';

comment on column students.student_care_assignment.role_id
  is 'ref_type = care_team_role (primary_therapist, secondary_therapist, coordinator, '
     'psychologist, consultant, observer, supervisor, ...). Configurable per-tenant.';
comment on column students.student_care_assignment.grant_type_id
  is 'ref_type = care_team_grant_type (permanent, temporary, delegated, substitute, emergency). '
     'NULL is treated as permanent. Determines whether reason is required and audit verbosity.';
comment on column students.student_care_assignment.source_assignment_id
  is 'Self-FK. Points to the original assignment being delegated, covered, or substituted. '
     'Populated for grant_type IN (delegated, substitute, emergency).';
comment on column students.student_care_assignment.reason
  is 'Justification text. Mandatory for emergency and delegated grant types; '
     'enforced at the application layer (Phase 4).';

-- ── Indexes for the planned Phase 3 RLS policy lookup ────────────────
-- Core RLS lookup: user_can_access_student() resolves educator.user_id = current_user_id()
create index ix_care_assignment_educator_student
  on students.student_care_assignment (educator_id, student_id)
  where status = 'active' and deleted_at is null;

-- Per-student care team listing and bulk app-layer pre-filter
create index ix_care_assignment_student
  on students.student_care_assignment (student_id, status)
  where deleted_at is null;

-- Effective-date window queries and historical access reports
create index ix_care_assignment_educator_active
  on students.student_care_assignment (educator_id, active_from, active_to)
  where status = 'active' and deleted_at is null;

-- Enforce at most one active primary assignment per student
create unique index uq_care_assignment_one_primary
  on students.student_care_assignment (student_id)
  where is_primary and status = 'active' and deleted_at is null;

-- ── Phase 3 helper function (production-ready, used by care_team_isolation policies) ──
-- SECURITY DEFINER: runs as table owner → bypasses RLS on student_care_assignment,
-- educator, and core.setting_value. This avoids recursive RLS evaluation.
-- search_path = '': all body references are fully schema-qualified (injection-safe).
create or replace function students.user_can_access_student(p_student_id uuid)
returns boolean
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_abac_enabled boolean;
begin
  -- No student context: allow (lead-stage assessment rows, orphaned data).
  if p_student_id is null then
    return true;
  end if;

  -- Bypass GUC: set by TenantConnectionInterceptor when JWT has care_team:bypass.
  -- Must derive only from verified claims — same trust model as app.current_corporation_id.
  if coalesce(
    nullif(current_setting('app.care_team_bypass', true), '')::boolean,
    false
  ) then
    return true;
  end if;

  -- Per-tenant rollout switch (default false = RBAC-only until tenant opts in).
  select coalesce((sv.value)::boolean, false)
    into v_abac_enabled
  from core.setting_value sv
  where sv.setting_key   = 'care_team_abac_enabled'
    and sv.scope_level   = 'corporation'
    and sv.corporation_id = core.current_corporation_id();

  if not coalesce(v_abac_enabled, false) then
    return true;
  end if;

  -- Full care-team membership check (uses ix_care_assignment_educator_student).
  return exists (
    select 1
    from students.student_care_assignment a
    join educators.educator e on e.id = a.educator_id
    where a.student_id  = p_student_id
      and e.user_id     = core.current_user_id()
      and a.status      = 'active'
      and a.deleted_at  is null
      and a.active_from <= now()
      and (a.active_to  is null or a.active_to > now())
  );
end;
$$;

comment on function students.user_can_access_student(uuid)
  is 'Phase 3 production function. Returns true if session user may access clinical data '
     'for p_student_id: bypass GUC → rollout switch → care-team EXISTS check. '
     'SECURITY DEFINER (runs as owner, bypasses RLS on referenced tables). '
     'See ABAC_CARE_TEAM_DESIGN.md §5.2 and Flyway V16.';

-- ── Setting definition for per-tenant ABAC rollout switch ────────────────────
-- This seed is idempotent. The default value false keeps all tenants in
-- RBAC-only mode until they insert a core.setting_value row to opt in.
insert into core.setting_definition (key, data_type, default_value, scope_levels, description)
values (
  'care_team_abac_enabled',
  'boolean',
  'false'::jsonb,
  '{corporation}',
  'Enables ABAC/Care-Team RESTRICTIVE RLS for clinical tables. '
  'Default false = RBAC-only. Enable per-tenant after backfill + assignment seeding. '
  'See ABAC_CARE_TEAM_DESIGN.md §8 Phase 3.'
)
on conflict (key) do nothing;
