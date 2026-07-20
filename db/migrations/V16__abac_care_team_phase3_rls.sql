-- =============================================================================
-- AyNesil Platform :: Flyway V16 — ABAC / Care-Team Phase 3: RLS Integration
-- =============================================================================
-- Implements ABAC_CARE_TEAM_DESIGN.md Phase 3 (RLS integration).
-- Purely ADDITIVE — no existing table/column/FK/policy is modified.
-- No backend code (Phase 4) or frontend changes (Phase 5) are included.
--
-- Objects created / changed:
--   1. setting_definition   : 'care_team_abac_enabled' (per-tenant rollout switch)
--   2. Function             : students.user_can_access_student() — replaces Phase 2 stub
--   3. RLS (structural)     : ENABLE ROW LEVEL SECURITY on assessment.assessment_response
--                             + PERMISSIVE base policy 'assessment_response_base_access'
--   4. Policies (×17)       : RESTRICTIVE 'care_team_isolation' on every clinical table
--                             listed in ABAC_CARE_TEAM_DESIGN.md §3.1
--
-- ── GUC wiring note (Phase 4 reference) ──────────────────────────────────────
-- The application's TenantConnectionInterceptor MUST set:
--
--   SET LOCAL app.care_team_bypass = 'true'   (only when JWT claims contain care_team:bypass)
--   SET LOCAL app.care_team_bypass = 'false'  (all other authenticated sessions)
--
-- 'LOCAL' scopes the GUC to the current transaction — identical discipline to
-- app.current_corporation_id and app.current_user_id.
-- Source of truth: verified perm claim from JWT, NOT any client-supplied header.
-- This wiring is NOT implemented in application code yet (Phase 4).
--
-- ── Per-tenant rollout logic inside user_can_access_student() ─────────────────
-- 1. If app.care_team_bypass = 'true'              → return true  (bypass)
-- 2. If care_team_abac_enabled = false (default)   → return true  (RBAC-only mode)
-- 3. Else                                          → EXISTS check on student_care_assignment
--
-- STRICTLY FORBIDDEN in this migration:
-- • Modifying existing policies (tenant_isolation etc.)
-- • Modifying existing tables / columns / FKs
-- • Application-layer code (Phase 4) or frontend (Phase 5)
-- • Disabling or replacing existing RBAC checks
--
-- Idempotent (ON CONFLICT DO NOTHING, DROP POLICY IF EXISTS, CREATE OR REPLACE).
-- Owner rolüyle çalışır — RLS bypass. Run ONLY after V15 (Phase 2 DDL) is applied.
-- =============================================================================


-- ── Step 0: Pre-flight assertions ─────────────────────────────────────────────
-- Abort immediately if Phase 2 objects are missing.

do $$
begin
  -- care_team:bypass permission must exist (V15 Step 8)
  if not exists (select 1 from iam.permission where code = 'care_team:bypass') then
    raise exception
      'PREFLIGHT FAIL: permission care_team:bypass not found. '
      'Apply V15 (Phase 2 DDL migration) before running V16.';
  end if;

  -- At least one role must hold care_team:bypass (V15 Step 10)
  if not exists (
    select 1 from iam.role_permission rp
    join iam.permission p on p.id = rp.permission_id
    where p.code = 'care_team:bypass'
  ) then
    raise exception
      'PREFLIGHT FAIL: no role_permission row grants care_team:bypass. '
      'Phase 2 seed (V15 Step 10) was not applied correctly.';
  end if;

  -- students.student_care_assignment must exist (V15 Step 1)
  if not exists (
    select 1 from information_schema.tables
    where table_schema = 'students' and table_name = 'student_care_assignment'
  ) then
    raise exception
      'PREFLIGHT FAIL: students.student_care_assignment does not exist. '
      'Apply V15 (Phase 2 DDL migration) before running V16.';
  end if;

  -- core.setting_definition must exist (baseline DDL, 05_platform_services.sql)
  if not exists (
    select 1 from information_schema.tables
    where table_schema = 'core' and table_name = 'setting_definition'
  ) then
    raise exception
      'PREFLIGHT FAIL: core.setting_definition does not exist. '
      'Baseline DDL (V1) must be applied first.';
  end if;

  raise notice 'PREFLIGHT OK — all Phase 2 objects confirmed present.';
end $$;


-- ── Step 1: Per-tenant rollout switch setting definition ──────────────────────
-- Inserting the key into setting_definition makes it discoverable by the
-- settings API and gives it a typed default. Tenants start with false (RBAC-only).
-- To enable ABAC for a tenant, INSERT a core.setting_value row:
--   INSERT INTO core.setting_value(setting_key, scope_level, corporation_id, value)
--   VALUES ('care_team_abac_enabled', 'corporation', '<corp_id>', 'true');

insert into core.setting_definition (key, data_type, default_value, scope_levels, description)
values (
  'care_team_abac_enabled',
  'boolean',
  'false'::jsonb,
  '{corporation}',
  'Enables ABAC/Care-Team row-level enforcement for clinical tables (RESTRICTIVE RLS). '
  'Default false = RBAC-only mode; set to true per-tenant only after: '
  '(a) all active educators have educator.user_id set, '
  '(b) initial care-team assignments are seeded per student, '
  '(c) privileged roles hold care_team:bypass. '
  'See ABAC_CARE_TEAM_DESIGN.md §8 Phase 3.'
)
on conflict (key) do nothing;


-- ── Step 2: Real user_can_access_student() function ───────────────────────────
-- Replaces the Phase 2 no-op stub.
-- SECURITY DEFINER: runs as the table owner, bypassing RLS on the tables it
-- queries (student_care_assignment, educator, setting_value). This prevents
-- recursive RLS evaluation and allows the function to remain a single source
-- of truth without needing BYPASSRLS on the app role.
-- search_path = '': all references are fully qualified (security best-practice).

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
  -- No student context: allow (e.g. lead-stage assessment records where student_id IS NULL).
  if p_student_id is null then
    return true;
  end if;

  -- Bypass GUC: set by TenantConnectionInterceptor when the verified JWT perm claim
  -- includes care_team:bypass. Must NOT derive from any client-supplied input.
  -- Identical trust model to app.current_corporation_id (Phase 4 will wire this).
  if coalesce(
    nullif(current_setting('app.care_team_bypass', true), '')::boolean,
    false
  ) then
    return true;
  end if;

  -- Per-tenant rollout switch: default false keeps all tenants in RBAC-only mode
  -- until they explicitly opt in by inserting a core.setting_value row.
  select coalesce((sv.value)::boolean, false)
    into v_abac_enabled
  from core.setting_value sv
  where sv.setting_key   = 'care_team_abac_enabled'
    and sv.scope_level   = 'corporation'
    and sv.corporation_id = core.current_corporation_id();

  if not coalesce(v_abac_enabled, false) then
    return true;  -- ABAC disabled for this tenant; behave as RBAC-only
  end if;

  -- Care-team membership check.
  -- Uses ix_care_assignment_educator_student (created in Phase 2, V15 Step 1).
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
  is 'Phase 3 production function. Returns true if the session user can access the given '
     'student''s clinical data: (1) NULL student_id → true (RBAC-only / lead-stage), '
     '(2) app.care_team_bypass = true → true (bypass), '
     '(3) care_team_abac_enabled = false for this tenant → true (rollout switch off), '
     '(4) EXISTS active care-team assignment matching current educator. '
     'SECURITY DEFINER — runs as table owner to avoid recursive RLS. '
     'Phase 4 wires the bypass GUC in TenantConnectionInterceptor.';


-- ── Step 3: RLS on assessment.assessment_response (no corporation_id) ─────────
-- assessment_response has no corporation_id, so 99_triggers_rls_policies.sql
-- did not enable RLS or add tenant_isolation on it. We must do so here explicitly.
-- The PERMISSIVE base policy enforces tenant isolation via the parent session.
-- The RESTRICTIVE care_team policy then narrows to care-team members.

alter table assessment.assessment_response enable row level security;

drop policy if exists assessment_response_base_access on assessment.assessment_response;
create policy assessment_response_base_access on assessment.assessment_response
  as permissive
  for all
  using (
    exists (
      select 1 from assessment.assessment_session s
      where s.id              = assessment_session_id
        and s.corporation_id  = core.current_corporation_id()
    )
  )
  with check (
    exists (
      select 1 from assessment.assessment_session s
      where s.id              = assessment_session_id
        and s.corporation_id  = core.current_corporation_id()
    )
  );


-- ── Step 4: RESTRICTIVE care_team_isolation on clinical tables ────────────────
--
-- Design §5.2: "the care-team policy MUST be RESTRICTIVE, not PERMISSIVE."
-- RESTRICTIVE policies are AND-combined with the existing PERMISSIVE tenant_isolation.
-- A row is visible ONLY when it passes BOTH tenant_isolation AND care_team_isolation.
--
-- students.student master row intentionally excluded (RBAC-only per design §3.1).
--
-- Pattern A — direct student_id                 : user_can_access_student(student_id)
-- Pattern B — nullable student_id               : IS NULL OR user_can_access_student(student_id)
-- Pattern C — indirect via assessment_session_id: EXISTS(parent session)
-- Pattern D — indirect via education_plan_id    : EXISTS(parent plan)
-- Pattern E — indirect via student_goal_id      : EXISTS(parent goal)
--
-- When care_team_abac_enabled = false (default) the function returns true
-- for every call → every RESTRICTIVE policy becomes a no-op → zero behavior change
-- until a tenant explicitly opts in.


-- ── 4a. students schema ───────────────────────────────────────────────────────

-- diagnosis
drop policy if exists care_team_isolation on students.diagnosis;
create policy care_team_isolation on students.diagnosis
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- developmental_profile
drop policy if exists care_team_isolation on students.developmental_profile;
create policy care_team_isolation on students.developmental_profile
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- medical_report
drop policy if exists care_team_isolation on students.medical_report;
create policy care_team_isolation on students.medical_report
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- development_report
drop policy if exists care_team_isolation on students.development_report;
create policy care_team_isolation on students.development_report
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- external_institution_report
drop policy if exists care_team_isolation on students.external_institution_report;
create policy care_team_isolation on students.external_institution_report
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- case_note
drop policy if exists care_team_isolation on students.case_note;
create policy care_team_isolation on students.case_note
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- student_guardian — care-team members need guardian contact for clinical coordination.
-- NOTE: if care_team_abac_enabled = true, guardian portal users (who are not educators)
-- will also be subject to this policy. Guardian portal views (v_portal_my_students etc.)
-- currently rely on guardian.user_id matching, not educator assignments. Verify guardian
-- portal screens before enabling ABAC for any tenant (ABAC_CARE_TEAM_DESIGN.md §9).
drop policy if exists care_team_isolation on students.student_guardian;
create policy care_team_isolation on students.student_guardian
  as restrictive for all
  using (students.user_can_access_student(student_id));


-- ── 4b. assessment schema ─────────────────────────────────────────────────────

-- assessment_session — student_id is nullable; NULL = lead-stage (RBAC-only)
drop policy if exists care_team_isolation on assessment.assessment_session;
create policy care_team_isolation on assessment.assessment_session
  as restrictive for all
  using (
    student_id is null
    or students.user_can_access_student(student_id)
  );

-- assessment_response — gated via parent session (Pattern C)
drop policy if exists care_team_isolation on assessment.assessment_response;
create policy care_team_isolation on assessment.assessment_response
  as restrictive for all
  using (
    exists (
      select 1 from assessment.assessment_session s
      where s.id = assessment_session_id
    )
  );

-- assessment_report — gated via parent session (Pattern C)
drop policy if exists care_team_isolation on assessment.assessment_report;
create policy care_team_isolation on assessment.assessment_report
  as restrictive for all
  using (
    exists (
      select 1 from assessment.assessment_session s
      where s.id = assessment_session_id
    )
  );


-- ── 4c. education schema ─────────────────────────────────────────────────────

-- student_goal
drop policy if exists care_team_isolation on education.student_goal;
create policy care_team_isolation on education.student_goal
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- education_plan
drop policy if exists care_team_isolation on education.education_plan;
create policy care_team_isolation on education.education_plan
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- education_plan_goal — gated via parent plan (Pattern D)
drop policy if exists care_team_isolation on education.education_plan_goal;
create policy care_team_isolation on education.education_plan_goal
  as restrictive for all
  using (
    exists (
      select 1 from education.education_plan ep
      where ep.id = education_plan_id
    )
  );

-- education_plan_review — gated via parent plan (Pattern D)
drop policy if exists care_team_isolation on education.education_plan_review;
create policy care_team_isolation on education.education_plan_review
  as restrictive for all
  using (
    exists (
      select 1 from education.education_plan ep
      where ep.id = education_plan_id
    )
  );

-- education_plan_approval — gated via parent plan (Pattern D)
drop policy if exists care_team_isolation on education.education_plan_approval;
create policy care_team_isolation on education.education_plan_approval
  as restrictive for all
  using (
    exists (
      select 1 from education.education_plan ep
      where ep.id = education_plan_id
    )
  );

-- education_plan_revision — gated via parent plan (Pattern D)
drop policy if exists care_team_isolation on education.education_plan_revision;
create policy care_team_isolation on education.education_plan_revision
  as restrictive for all
  using (
    exists (
      select 1 from education.education_plan ep
      where ep.id = education_plan_id
    )
  );

-- goal_progress — gated via parent student_goal (Pattern E)
drop policy if exists care_team_isolation on education.goal_progress;
create policy care_team_isolation on education.goal_progress
  as restrictive for all
  using (
    exists (
      select 1 from education.student_goal sg
      where sg.id = student_goal_id
    )
  );


-- ── Step 5: Final assertion — confirm all 17 care_team_isolation policies exist ─

do $$
declare
  v_pol_count int;
begin
  select count(*) into v_pol_count
  from pg_policies
  where policyname = 'care_team_isolation';

  if v_pol_count < 17 then
    raise exception
      'POST-MIGRATION ASSERT FAIL: expected >= 17 care_team_isolation policies, '
      'found %. Check for policy creation errors above.', v_pol_count;
  end if;

  raise notice
    'V16 COMPLETE — ABAC Phase 3 applied: '
    '1 setting_definition, '
    'user_can_access_student() real function, '
    '1 RLS enable + 1 base policy on assessment_response, '
    '% care_team_isolation RESTRICTIVE policies across clinical tables. '
    'All tenants start with care_team_abac_enabled = false (RBAC-only mode). '
    'Enable per-tenant: INSERT INTO core.setting_value(setting_key, scope_level, '
    'corporation_id, value) VALUES (''care_team_abac_enabled'', ''corporation'', '
    '''<corp_id>'', ''true'');',
    v_pol_count;
end $$;
