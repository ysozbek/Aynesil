-- =============================================================================
-- AyNesil Platform :: Flyway V15 — ABAC / Care-Team Phase 2: DDL Migration
-- =============================================================================
-- Implements ABAC_CARE_TEAM_DESIGN.md Phase 2 (DDL migration, no RLS policies yet).
-- This migration is purely ADDITIVE — no existing table, column, or FK is modified.
--
-- Objects created / seeded:
--   1. Table:       students.student_care_assignment (+ 4 indexes)
--   2. RLS:         tenant_isolation policy on students.student_care_assignment
--   3. Triggers:    trg_set_updated_at, trg_audit on students.student_care_assignment
--   4. Function:    students.user_can_access_student() — Phase 3 no-op stub
--   5. Ref types:   care_team_role, care_team_grant_type  (2 new ref_types)
--   6. Ref values:  7 care_team_role + 5 care_team_grant_type values  (12 total)
--   7. Translations: tr + en for all 12 new ref_values  (24 rows)
--   8. Permissions: care_team:read, care_team:assign, care_team:bypass  (3 new)
--   9. Menu:        students-care-team sub-item under 'students'  (1 item + 2 translations)
--  10. Role grants: all 3 care_team:* permissions → admin role
--  11. Backfill check: NOTICE/WARNING for active educators missing user_id
--
-- Phase 3 (RLS enforcement) must NOT go live until:
--   a) Every active educator has educator.user_id populated (see Step 11 check).
--   b) Initial care-team assignments are seeded for each active student.
--   c) user_can_access_student() body is replaced with the real EXISTS check.
--
-- Strictly forbidden: no RLS policy creation (Phase 3), no existing-table changes.
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================


-- ── Step 1: Care-team assignment table ───────────────────────────────────────

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

-- Indexes for the planned Phase 3 RLS policy lookup
create index ix_care_assignment_educator_student
  on students.student_care_assignment (educator_id, student_id)
  where status = 'active' and deleted_at is null;

create index ix_care_assignment_student
  on students.student_care_assignment (student_id, status)
  where deleted_at is null;

create index ix_care_assignment_educator_active
  on students.student_care_assignment (educator_id, active_from, active_to)
  where status = 'active' and deleted_at is null;

-- At most one active primary assignment per student
create unique index uq_care_assignment_one_primary
  on students.student_care_assignment (student_id)
  where is_primary and status = 'active' and deleted_at is null;


-- ── Step 2: RLS — tenant isolation ───────────────────────────────────────────
-- Mirrors the DO block in db/99_triggers_rls_policies.sql §2.
-- The dynamic DO block runs only once (during the V1 baseline or smoke_test.sh);
-- new tables created in incremental migrations must wire this explicitly.

alter table students.student_care_assignment enable row level security;

drop policy if exists tenant_isolation on students.student_care_assignment;
create policy tenant_isolation on students.student_care_assignment
  using  (corporation_id is null or corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());


-- ── Step 3: Triggers ─────────────────────────────────────────────────────────
-- Mirrors the DO blocks in db/99_triggers_rls_policies.sql §1 and §3.

create or replace trigger trg_set_updated_at
  before update on students.student_care_assignment
  for each row execute function core.set_updated_at();

create or replace trigger trg_audit
  after insert or update or delete on students.student_care_assignment
  for each row execute function core.audit_trigger();


-- ── Step 4: Phase 3 hook — user_can_access_student() stub ────────────────────
-- Phase 3 will replace this body with the real care-team EXISTS check and attach
-- RESTRICTIVE policies on every clinical table in ABAC_CARE_TEAM_DESIGN.md §3.1.
-- Until then this function always returns TRUE (no row restriction applied).
-- DO NOT attach this stub to any RLS policy before Phase 3 updates the body.

create or replace function students.user_can_access_student(p_student_id uuid)
returns boolean
language sql stable
as $$
  -- STUB (Phase 2 only). Phase 3 will replace this with:
  --   SELECT EXISTS (
  --     SELECT 1
  --     FROM students.student_care_assignment a
  --     JOIN educators.educator e ON e.id = a.educator_id
  --     WHERE a.student_id = p_student_id
  --       AND e.user_id = core.current_user_id()
  --       AND a.status = 'active'
  --       AND a.deleted_at IS NULL
  --       AND a.active_from <= now()
  --       AND (a.active_to IS NULL OR a.active_to > now())
  --   );
  select true;
$$;

comment on function students.user_can_access_student(uuid)
  is 'ABAC Phase 2 stub — always TRUE, no enforcement yet. '
     'Phase 3 replaces this body with the care-team EXISTS check and attaches '
     'RESTRICTIVE RLS policies on clinical tables (ABAC_CARE_TEAM_DESIGN.md §5.2). '
     'Do not rely on this returning FALSE for any security purpose until Phase 3.';


-- ── Step 5: Reference types ───────────────────────────────────────────────────

insert into ref.ref_type (code, name, is_system, is_hierarchical, allows_tenant_values) values
  ('care_team_role',
   'Care Team Roles',
   false, false, true),
  ('care_team_grant_type',
   'Care Team Grant Types',
   false, false, true)
on conflict (code) do nothing;


-- ── Step 6: Reference values ──────────────────────────────────────────────────

insert into ref.ref_value (ref_type_id, code, sort_order, is_default, is_system)
select ref.type_id(v.type_code), v.code, v.sort_order, v.is_default, v.is_system
from (values
  -- care_team_role — configurable clinical roles; tenants may extend/deactivate
  ('care_team_role', 'primary_therapist',    1, true,  false),
  ('care_team_role', 'secondary_therapist',  2, false, false),
  ('care_team_role', 'coordinator',          3, false, false),
  ('care_team_role', 'psychologist',         4, false, false),
  ('care_team_role', 'consultant',           5, false, false),
  ('care_team_role', 'observer',             6, false, false),
  ('care_team_role', 'supervisor',           7, false, false),
  -- care_team_grant_type — structural access patterns; platform-owned (is_system=true)
  ('care_team_grant_type', 'permanent',   1, true,  true),
  ('care_team_grant_type', 'temporary',   2, false, true),
  ('care_team_grant_type', 'delegated',   3, false, true),
  ('care_team_grant_type', 'substitute',  4, false, true),
  ('care_team_grant_type', 'emergency',   5, false, true)
) as v (type_code, code, sort_order, is_default, is_system)
on conflict do nothing;


-- ── Step 7: Translations (tr + en) ───────────────────────────────────────────

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  ('care_team_role', 'primary_therapist',   'tr', 'Birincil Terapist'),
  ('care_team_role', 'primary_therapist',   'en', 'Primary Therapist'),
  ('care_team_role', 'secondary_therapist', 'tr', 'İkincil Terapist'),
  ('care_team_role', 'secondary_therapist', 'en', 'Secondary Therapist'),
  ('care_team_role', 'coordinator',         'tr', 'Koordinatör'),
  ('care_team_role', 'coordinator',         'en', 'Coordinator'),
  ('care_team_role', 'psychologist',        'tr', 'Psikolog'),
  ('care_team_role', 'psychologist',        'en', 'Psychologist'),
  ('care_team_role', 'consultant',          'tr', 'Danışman'),
  ('care_team_role', 'consultant',          'en', 'Consultant'),
  ('care_team_role', 'observer',            'tr', 'Gözlemci'),
  ('care_team_role', 'observer',            'en', 'Observer'),
  ('care_team_role', 'supervisor',          'tr', 'Süpervizör'),
  ('care_team_role', 'supervisor',          'en', 'Supervisor'),
  ('care_team_grant_type', 'permanent',     'tr', 'Kalıcı'),
  ('care_team_grant_type', 'permanent',     'en', 'Permanent'),
  ('care_team_grant_type', 'temporary',     'tr', 'Geçici'),
  ('care_team_grant_type', 'temporary',     'en', 'Temporary'),
  ('care_team_grant_type', 'delegated',     'tr', 'Devredilen'),
  ('care_team_grant_type', 'delegated',     'en', 'Delegated'),
  ('care_team_grant_type', 'substitute',    'tr', 'Vekil'),
  ('care_team_grant_type', 'substitute',    'en', 'Substitute'),
  ('care_team_grant_type', 'emergency',     'tr', 'Acil Erişim'),
  ('care_team_grant_type', 'emergency',     'en', 'Emergency')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code        = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 8: Permission catalog ────────────────────────────────────────────────
-- care_team:bypass is the critical gate for Phase 3 (GUC-based bypass in RLS).
-- care_team:read / care_team:assign gate assignment management UI (Phase 4/5).
-- Following the existing pattern (V6, V10, V12, V14 — resource:action format).

insert into iam.permission (code, resource, action) values
  ('care_team:read',   'care_team', 'read'),
  ('care_team:assign', 'care_team', 'assign'),
  ('care_team:bypass', 'care_team', 'bypass')
on conflict (code) do nothing;


-- ── Step 9: Care team management menu item (sub-item under 'students') ────────

insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select
    null,
    parent.id,
    v.code,
    v.route,
    v.icon,
    v.sort_order,
    p.id,
    true
from (values
    ('students-care-team', '/students/care-team', 'user-group', 60, 'care_team:assign')
) as v (code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'students' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('students-care-team', 'tr', 'Bakım Ekibi'),
    ('students-care-team', 'en', 'Care Team')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;


-- ── Step 10: Grant care_team:* permissions to the admin role ─────────────────
-- Grants to ALL roles named 'admin' across all tenants (same pattern as V14).
-- Coordinator roles, when created, should also receive at minimum care_team:bypass
-- (campus-scoped bypass is enforced via the existing user_role.campus_id mechanism,
-- not via a separate column — design §4.2).

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where p.code in ('care_team:read', 'care_team:assign', 'care_team:bypass')
  and r.name = 'admin'
on conflict do nothing;


-- ── Step 11: Backfill check — educator.user_id gaps ──────────────────────────
-- Design §9 risk: "Clinicians without a linked user cannot be matched."
-- This block raises a WARNING listing every active educator who lacks a user_id.
-- The migration succeeds regardless; the WARNING is a pre-flight advisory.
-- Phase 3 (RLS) MUST NOT be deployed until all active educators have user_id set.

do $$
declare
  v_count int;
  r       record;
begin
  select count(*) into v_count
  from educators.educator
  where user_id is null
    and is_active = true
    and deleted_at is null;

  if v_count = 0 then
    raise notice
      'BACKFILL CHECK OK — all active educators have user_id set. '
      'Phase 3 is safe to proceed (from an educator-identity perspective).';
  else
    raise warning
      'BACKFILL NEEDED — % active educator(s) lack educator.user_id. '
      'These educators cannot be matched to care-team assignments and will be '
      'locked out of clinical data the moment Phase 3 RLS goes live. '
      'Resolve before deploying Phase 3 (ABAC_CARE_TEAM_DESIGN.md §9 risk #3).',
      v_count;

    for r in
      select id,
             first_name || ' ' || last_name as full_name,
             coalesce(email, '(no email)')  as email,
             corporation_id
      from educators.educator
      where user_id is null
        and is_active = true
        and deleted_at is null
      order by corporation_id, last_name, first_name
    loop
      raise warning '  → educator id=% name=% email=% corporation=%',
        r.id, r.full_name, r.email, r.corporation_id;
    end loop;
  end if;
end $$;
