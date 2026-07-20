-- =====================================================================
-- AyNesil Platform :: ABAC Phase 3 — Behavioral Verification
-- =====================================================================
-- Validates the four ABAC scenarios required by ABAC_CARE_TEAM_DESIGN.md §8:
--   (a) assigned educator  → sees ONLY their students' clinical rows
--   (b) unassigned educator → sees 0 rows
--   (c) bypass role        → sees ALL tenant rows
--   (d) no tenant context  → sees 0 rows (tenant_isolation guarantee preserved)
--
-- Strategy: seeds test data as postgres (owner bypasses RLS), passes
-- UUIDs via transaction-local GUCs, then impersonates aynesil_app with
-- SET SESSION AUTHORIZATION to subject queries to full RLS enforcement.
-- ALL test data is rolled back at the end — safe to run repeatedly.
--
-- Usage:
--   psql -v ON_ERROR_STOP=1 -U postgres -d akran \
--        -f /repo/db/validation/verify_abac_phase3.sql
-- =====================================================================
\pset pager off

begin;

-- ── 0. Ensure aynesil_app role exists and has table-level access ─────────────
do $$
begin
  if not exists (select 1 from pg_roles where rolname = 'aynesil_app') then
    create role aynesil_app;
  end if;
end $$;

grant usage on schema students, assessment, education, educators, core, iam, ref
  to aynesil_app;
grant select, insert, update, delete on all tables in schema
  students, assessment, education, educators, core, iam, ref
  to aynesil_app;
grant execute on function students.user_can_access_student(uuid)  to aynesil_app;
grant execute on function core.current_user_id()                  to aynesil_app;
grant execute on function core.current_corporation_id()           to aynesil_app;
grant execute on function ref.type_id(text)                       to aynesil_app;


-- ── 1. Seed test data and store UUIDs in transaction-local GUCs ─────────────
-- postgres (owner) bypasses RLS so this INSERT works without tenant context.
do $$
declare
  v_corp_id         uuid;
  v_campus_id       uuid;
  v_user_assigned   uuid;
  v_user_unassigned uuid;
  v_educator_asgn   uuid;
  v_student_id      uuid;
  v_role_id         uuid;
begin
  select id into strict v_corp_id  from core.corporation where code = 'akran';
  select id into        v_campus_id from core.campus where corporation_id = v_corp_id limit 1;

  -- Enable ABAC for this test run
  insert into core.setting_value(setting_key, scope_level, corporation_id, value)
  values ('care_team_abac_enabled', 'corporation', v_corp_id, 'true'::jsonb)
  on conflict (setting_key, scope_level, corporation_id, scope_id)
  do update set value = 'true'::jsonb;

  -- Two test user accounts
  insert into iam.user_account(corporation_id, username, full_name, status)
  values (v_corp_id, '__p3_user_asgn__',   'P3 Assigned',   'active')
  returning id into v_user_assigned;

  insert into iam.user_account(corporation_id, username, full_name, status)
  values (v_corp_id, '__p3_user_unasgn__', 'P3 Unassigned', 'active')
  returning id into v_user_unassigned;

  -- Educator profiles linked to those users
  insert into educators.educator(corporation_id, user_id, first_name, last_name, is_active)
  values (v_corp_id, v_user_assigned, 'P3Asgn', 'Edu', true)
  returning id into v_educator_asgn;

  insert into educators.educator(corporation_id, user_id, first_name, last_name, is_active)
  values (v_corp_id, v_user_unassigned, 'P3Unasgn', 'Edu', true);

  -- One test student
  insert into students.student(corporation_id, first_name, last_name, primary_campus_id)
  values (v_corp_id, '__P3Test__', 'Student', v_campus_id)
  returning id into v_student_id;

  -- One clinical row: diagnosis
  insert into students.diagnosis(corporation_id, student_id, description, diagnosed_on)
  values (v_corp_id, v_student_id, 'ABAC Phase 3 verification row', current_date);

  -- Resolve primary_therapist ref value
  select rv.id into v_role_id
  from ref.ref_value rv
  where rv.ref_type_id   = ref.type_id('care_team_role')
    and rv.code          = 'primary_therapist'
    and rv.corporation_id is null;

  -- Assign ONLY the first educator to the student
  insert into students.student_care_assignment(
    corporation_id, student_id, educator_id, role_id, status, active_from
  ) values (v_corp_id, v_student_id, v_educator_asgn, v_role_id, 'active', current_date);

  -- Pass IDs to verification block via transaction-local GUCs
  perform set_config('app._p3_corp_id',    v_corp_id::text,         true);
  perform set_config('app._p3_user_asgn',  v_user_assigned::text,   true);
  perform set_config('app._p3_user_unasgn',v_user_unassigned::text, true);
end $$;


-- ── 2. Behavioral verification as aynesil_app (full RLS enforcement) ─────────
set local session authorization aynesil_app;

do $$
declare
  v_corp_id         uuid := current_setting('app._p3_corp_id')::uuid;
  v_user_assigned   uuid := current_setting('app._p3_user_asgn')::uuid;
  v_user_unassigned uuid := current_setting('app._p3_user_unasgn')::uuid;
  v_count           int;
  v_all_pass        boolean := true;
begin
  -- ── (a) Assigned educator sees exactly 1 row ──────────────────────────────
  perform set_config('app.current_corporation_id', v_corp_id::text,      true);
  perform set_config('app.current_user_id',        v_user_assigned::text, true);
  perform set_config('app.care_team_bypass',       'false',               true);

  select count(*) into v_count from students.diagnosis;
  if v_count = 1 then
    raise notice '(a) PASS — assigned educator sees % diagnosis row (expected 1)', v_count;
  else
    raise warning '(a) FAIL — assigned educator sees % rows, expected 1', v_count;
    v_all_pass := false;
  end if;

  -- ── (b) Unassigned educator sees 0 rows ───────────────────────────────────
  perform set_config('app.current_user_id', v_user_unassigned::text, true);

  select count(*) into v_count from students.diagnosis;
  if v_count = 0 then
    raise notice '(b) PASS — unassigned educator sees % rows (expected 0)', v_count;
  else
    raise warning '(b) FAIL — unassigned educator sees % rows, expected 0', v_count;
    v_all_pass := false;
  end if;

  -- ── (c) Bypass role sees all tenant rows ──────────────────────────────────
  perform set_config('app.care_team_bypass', 'true', true);

  select count(*) into v_count from students.diagnosis;
  if v_count >= 1 then
    raise notice '(c) PASS — bypass user sees % row(s) (expected >= 1)', v_count;
  else
    raise warning '(c) FAIL — bypass user sees % rows, expected >= 1', v_count;
    v_all_pass := false;
  end if;

  -- ── (d) No tenant context → 0 rows (tenant_isolation guarantee) ──────────
  perform set_config('app.current_corporation_id', '', true);
  perform set_config('app.care_team_bypass',       'false', true);

  select count(*) into v_count from students.diagnosis;
  if v_count = 0 then
    raise notice '(d) PASS — no-tenant context sees % rows (expected 0)', v_count;
  else
    raise warning '(d) FAIL — no-tenant context sees % rows, expected 0', v_count;
    v_all_pass := false;
  end if;

  -- ── Summary ────────────────────────────────────────────────────────────────
  if v_all_pass then
    raise notice '';
    raise notice '============================================================';
    raise notice 'ABAC PHASE 3 BEHAVIORAL VERIFICATION: ALL 4 SCENARIOS PASSED';
    raise notice '============================================================';
  else
    raise exception
      'ABAC PHASE 3 BEHAVIORAL VERIFICATION: ONE OR MORE SCENARIOS FAILED '
      '(see WARN lines above)';
  end if;
end $$;

-- ── 3. Restore owner session ──────────────────────────────────────────────────
reset session authorization;


-- ── 4. Roll back all test data ────────────────────────────────────────────────
rollback;

\echo ''
\echo 'verify_abac_phase3.sql complete — all test data rolled back.'
