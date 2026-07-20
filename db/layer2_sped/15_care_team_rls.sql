-- =====================================================================
-- AyNesil Platform :: Layer 2 — Care Team RESTRICTIVE RLS Policies
-- ABAC Phase 3 — attaches care_team_isolation on every clinical table
-- listed in ABAC_CARE_TEAM_DESIGN.md §3.1.
--
-- This file runs AFTER db/99_triggers_rls_policies.sql in smoke_test.sh
-- (so tenant_isolation PERMISSIVE policies already exist on all tables
-- with corporation_id before we add the RESTRICTIVE layer on top).
--
-- Flyway equivalent: db/migrations/V16__abac_care_team_phase3_rls.sql
--
-- All tenants start with care_team_abac_enabled = false (RBAC-only mode).
-- The RESTRICTIVE policies become effective only when a tenant inserts:
--   INSERT INTO core.setting_value(setting_key, scope_level, corporation_id, value)
--   VALUES ('care_team_abac_enabled', 'corporation', '<corp_id>', 'true');
-- =====================================================================

-- ── assessment.assessment_response — enable RLS + base permissive policy ──────
-- This table has no corporation_id, so 99_triggers_rls_policies.sql skipped it.
-- The PERMISSIVE base policy enforces tenant isolation via the parent session.

alter table assessment.assessment_response enable row level security;

drop policy if exists assessment_response_base_access on assessment.assessment_response;
create policy assessment_response_base_access on assessment.assessment_response
  as permissive
  for all
  using (
    exists (
      select 1 from assessment.assessment_session s
      where s.id             = assessment_session_id
        and s.corporation_id = core.current_corporation_id()
    )
  )
  with check (
    exists (
      select 1 from assessment.assessment_session s
      where s.id             = assessment_session_id
        and s.corporation_id = core.current_corporation_id()
    )
  );


-- ── RESTRICTIVE care_team_isolation on all clinical tables ────────────────────
-- Pattern A: direct student_id
-- Pattern B: nullable student_id (assessment_session, lead-stage = null = RBAC-only)
-- Pattern C: indirect via assessment_session_id
-- Pattern D: indirect via education_plan_id
-- Pattern E: indirect via student_goal_id
--
-- students.student master row intentionally excluded (RBAC-only per design §3.1).

-- students.diagnosis (A)
drop policy if exists care_team_isolation on students.diagnosis;
create policy care_team_isolation on students.diagnosis
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- students.developmental_profile (A)
drop policy if exists care_team_isolation on students.developmental_profile;
create policy care_team_isolation on students.developmental_profile
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- students.medical_report (A)
drop policy if exists care_team_isolation on students.medical_report;
create policy care_team_isolation on students.medical_report
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- students.development_report (A)
drop policy if exists care_team_isolation on students.development_report;
create policy care_team_isolation on students.development_report
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- students.external_institution_report (A)
drop policy if exists care_team_isolation on students.external_institution_report;
create policy care_team_isolation on students.external_institution_report
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- students.case_note (A)
drop policy if exists care_team_isolation on students.case_note;
create policy care_team_isolation on students.case_note
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- students.student_guardian (A)
-- WARNING: when care_team_abac_enabled = true, guardian portal users (non-educators)
-- are also subject to this policy. Verify guardian portal screens before enabling ABAC.
drop policy if exists care_team_isolation on students.student_guardian;
create policy care_team_isolation on students.student_guardian
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- assessment.assessment_session (B) — student_id nullable; NULL = lead-stage
drop policy if exists care_team_isolation on assessment.assessment_session;
create policy care_team_isolation on assessment.assessment_session
  as restrictive for all
  using (
    student_id is null
    or students.user_can_access_student(student_id)
  );

-- assessment.assessment_response (C) — gated via parent session
drop policy if exists care_team_isolation on assessment.assessment_response;
create policy care_team_isolation on assessment.assessment_response
  as restrictive for all
  using (
    exists (
      select 1 from assessment.assessment_session s
      where s.id = assessment_session_id
    )
  );

-- assessment.assessment_report (C) — gated via parent session
drop policy if exists care_team_isolation on assessment.assessment_report;
create policy care_team_isolation on assessment.assessment_report
  as restrictive for all
  using (
    exists (
      select 1 from assessment.assessment_session s
      where s.id = assessment_session_id
    )
  );

-- education.student_goal (A)
drop policy if exists care_team_isolation on education.student_goal;
create policy care_team_isolation on education.student_goal
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- education.education_plan (A)
drop policy if exists care_team_isolation on education.education_plan;
create policy care_team_isolation on education.education_plan
  as restrictive for all
  using (students.user_can_access_student(student_id));

-- education.education_plan_goal (D) — gated via parent plan
drop policy if exists care_team_isolation on education.education_plan_goal;
create policy care_team_isolation on education.education_plan_goal
  as restrictive for all
  using (
    exists (
      select 1 from education.education_plan ep
      where ep.id = education_plan_id
    )
  );

-- education.education_plan_review (D) — gated via parent plan
drop policy if exists care_team_isolation on education.education_plan_review;
create policy care_team_isolation on education.education_plan_review
  as restrictive for all
  using (
    exists (
      select 1 from education.education_plan ep
      where ep.id = education_plan_id
    )
  );

-- education.education_plan_approval (D) — gated via parent plan
drop policy if exists care_team_isolation on education.education_plan_approval;
create policy care_team_isolation on education.education_plan_approval
  as restrictive for all
  using (
    exists (
      select 1 from education.education_plan ep
      where ep.id = education_plan_id
    )
  );

-- education.education_plan_revision (D) — gated via parent plan
drop policy if exists care_team_isolation on education.education_plan_revision;
create policy care_team_isolation on education.education_plan_revision
  as restrictive for all
  using (
    exists (
      select 1 from education.education_plan ep
      where ep.id = education_plan_id
    )
  );

-- education.goal_progress (E) — gated via parent student_goal
drop policy if exists care_team_isolation on education.goal_progress;
create policy care_team_isolation on education.goal_progress
  as restrictive for all
  using (
    exists (
      select 1 from education.student_goal sg
      where sg.id = student_goal_id
    )
  );
