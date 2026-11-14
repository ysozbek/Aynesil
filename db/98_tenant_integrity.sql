-- =============================================================================
-- AyNesil Platform :: 98 — Tenant integrity (composite FKs + global-or-tenant refs)
-- Target: PostgreSQL 15+ (ON DELETE SET NULL (column) form). Run after all layer files and
-- before 99_triggers_rls_policies.sql. Source-of-truth mirror of migrations V38–V41 (#86).
--
-- Layer files declare FKs inline as `col uuid references parent(id)`; this file turns every FK
-- between two tenant tables into FOREIGN KEY (corporation_id, col) REFERENCES
-- parent (corporation_id, id), keeping the ON DELETE action, and installs the
-- global-or-tenant trigger for references to tables that also hold global rows.
-- Keeping it in one cross-cutting file (like 99_*) avoids rewriting ~200 inline references
-- across 16 files while yielding the same end-state schema as the migrations.
-- =============================================================================

-- ── Unique (corporation_id, id) keys on referenced tables ──
-- assessment
create unique index if not exists uq_assessment_session_corp_id on assessment.assessment_session (corporation_id, id);

-- camps
create unique index if not exists uq_camp_corp_id on camps.camp (corporation_id, id);
create unique index if not exists uq_camp_activity_corp_id on camps.camp_activity (corporation_id, id);
create unique index if not exists uq_camp_enrollment_corp_id on camps.camp_enrollment (corporation_id, id);
create unique index if not exists uq_camp_period_corp_id on camps.camp_period (corporation_id, id);

-- consultancy
create unique index if not exists uq_consultancy_plan_corp_id on consultancy.consultancy_plan (corporation_id, id);
create unique index if not exists uq_institution_corp_id on consultancy.institution (corporation_id, id);
create unique index if not exists uq_observation_record_corp_id on consultancy.observation_record (corporation_id, id);
create unique index if not exists uq_school_visit_corp_id on consultancy.school_visit (corporation_id, id);

-- core
create unique index if not exists uq_campus_corp_id on core.campus (corporation_id, id);
create unique index if not exists uq_file_object_corp_id on core.file_object (corporation_id, id);
create unique index if not exists uq_integration_connection_corp_id on core.integration_connection (corporation_id, id);

-- crm
create unique index if not exists uq_lead_corp_id on crm.lead (corporation_id, id);

-- education
create unique index if not exists uq_academic_period_corp_id on education.academic_period (corporation_id, id);
create unique index if not exists uq_education_plan_corp_id on education.education_plan (corporation_id, id);
create unique index if not exists uq_enrollment_corp_id on education.enrollment (corporation_id, id);
create unique index if not exists uq_program_corp_id on education.program (corporation_id, id);
create unique index if not exists uq_program_service_corp_id on education.program_service (corporation_id, id);
create unique index if not exists uq_student_goal_corp_id on education.student_goal (corporation_id, id);
create unique index if not exists uq_student_program_corp_id on education.student_program (corporation_id, id);

-- educators
create unique index if not exists uq_educator_corp_id on educators.educator (corporation_id, id);

-- finance
create unique index if not exists uq_invoice_corp_id on finance.invoice (corporation_id, id);
create unique index if not exists uq_package_definition_corp_id on finance.package_definition (corporation_id, id);
create unique index if not exists uq_payment_corp_id on finance.payment (corporation_id, id);
create unique index if not exists uq_student_package_corp_id on finance.student_package (corporation_id, id);

-- iam
create unique index if not exists uq_user_account_corp_id on iam.user_account (corporation_id, id);

-- legal
create unique index if not exists uq_consent_template_corp_id on legal.consent_template (corporation_id, id);
create unique index if not exists uq_contract_template_corp_id on legal.contract_template (corporation_id, id);
create unique index if not exists uq_student_consent_corp_id on legal.student_consent (corporation_id, id);

-- media
create unique index if not exists uq_camera_corp_id on media.camera (corporation_id, id);

-- ops
create unique index if not exists uq_leave_request_corp_id on ops.leave_request (corporation_id, id);
create unique index if not exists uq_meeting_corp_id on ops.meeting (corporation_id, id);
create unique index if not exists uq_survey_definition_corp_id on ops.survey_definition (corporation_id, id);

-- scheduling
create unique index if not exists uq_recurring_schedule_corp_id on scheduling.recurring_schedule (corporation_id, id);
create unique index if not exists uq_room_corp_id on scheduling.room (corporation_id, id);
create unique index if not exists uq_session_corp_id on scheduling.session (corporation_id, id);

-- students
create unique index if not exists uq_guardian_corp_id on students.guardian (corporation_id, id);
create unique index if not exists uq_student_corp_id on students.student (corporation_id, id);
create unique index if not exists uq_student_care_assignment_corp_id on students.student_care_assignment (corporation_id, id);

-- ── Composite tenant FKs ──

-- Swaps a single-column FK for a tenant-scoped composite FK, keeping ON DELETE.
-- Idempotent: returns early when the composite FK already exists. Fails loudly if the
-- expected original constraint is missing or its ON DELETE action drifted.
create or replace function pg_temp.to_tenant_fk(
    p_child regclass, p_col name, p_parent regclass,
    p_old name, p_new name, p_expected_deltype "char", p_on_delete text, p_not_valid boolean)
returns void language plpgsql as $fn$
declare
    v_deltype "char";
begin
    if exists (select 1 from pg_constraint where conrelid = p_child and conname = p_new) then
        return;
    end if;
    select confdeltype into v_deltype
      from pg_constraint where conrelid = p_child and conname = p_old and contype = 'f';
    if v_deltype is null then
        raise exception 'tenant FK: expected constraint % on % not found', p_old, p_child;
    end if;
    if v_deltype <> p_expected_deltype then
        raise exception 'tenant FK: % on % has ON DELETE %, expected %', p_old, p_child, v_deltype, p_expected_deltype;
    end if;
    execute format('alter table %s drop constraint %I', p_child, p_old);
    execute format('alter table %s add constraint %I foreign key (corporation_id, %I) '
                   'references %s (corporation_id, id) on delete %s%s',
                   p_child, p_new, p_col, p_parent, p_on_delete,
                   case when p_not_valid then ' not valid' else '' end);
end
$fn$;

-- ── assessment
select pg_temp.to_tenant_fk('assessment.assessment_report', 'assessment_session_id', 'assessment.assessment_session', 'assessment_report_assessment_session_id_fkey', 'fk_assessment_report_assessment_session_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('assessment.assessment_report', 'file_id', 'core.file_object', 'assessment_report_file_id_fkey', 'fk_assessment_report_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('assessment.assessment_session', 'assessor_id', 'educators.educator', 'assessment_session_assessor_id_fkey', 'fk_assessment_session_assessor_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('assessment.assessment_session', 'campus_id', 'core.campus', 'assessment_session_campus_id_fkey', 'fk_assessment_session_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('assessment.assessment_session', 'lead_id', 'crm.lead', 'assessment_session_lead_id_fkey', 'fk_assessment_session_lead_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('assessment.assessment_session', 'student_id', 'students.student', 'assessment_session_student_id_fkey', 'fk_assessment_session_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('assessment.program_recommendation', 'assessment_session_id', 'assessment.assessment_session', 'program_recommendation_assessment_session_id_fkey', 'fk_program_recommendation_assessment_session_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('assessment.program_recommendation', 'lead_id', 'crm.lead', 'program_recommendation_lead_id_fkey', 'fk_program_recommendation_lead_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('assessment.program_recommendation', 'recommended_by', 'educators.educator', 'program_recommendation_recommended_by_fkey', 'fk_program_recommendation_recommended_by_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('assessment.program_recommendation', 'student_id', 'students.student', 'program_recommendation_student_id_fkey', 'fk_program_recommendation_student_id_tnt', 'a', 'NO ACTION', false);

-- ── camps
select pg_temp.to_tenant_fk('camps.camp', 'campus_id', 'core.campus', 'camp_campus_id_fkey', 'fk_camp_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('camps.camp_activity', 'camp_period_id', 'camps.camp_period', 'camp_activity_camp_period_id_fkey', 'fk_camp_activity_camp_period_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_activity', 'session_id', 'scheduling.session', 'camp_activity_session_id_fkey', 'fk_camp_activity_session_id_tnt', 'n', 'SET NULL (session_id)', false);
select pg_temp.to_tenant_fk('camps.camp_activity_participation', 'camp_activity_id', 'camps.camp_activity', 'camp_activity_participation_camp_activity_id_fkey', 'fk_camp_activity_participation_camp_activity_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_activity_participation', 'camp_enrollment_id', 'camps.camp_enrollment', 'camp_activity_participation_camp_enrollment_id_fkey', 'fk_camp_activity_participation_camp_enrollment_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_attendance', 'camp_enrollment_id', 'camps.camp_enrollment', 'camp_attendance_camp_enrollment_id_fkey', 'fk_camp_attendance_camp_enrollment_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_educator', 'camp_activity_id', 'camps.camp_activity', 'camp_educator_camp_activity_id_fkey', 'fk_camp_educator_camp_activity_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_educator', 'camp_id', 'camps.camp', 'camp_educator_camp_id_fkey', 'fk_camp_educator_camp_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_educator', 'camp_period_id', 'camps.camp_period', 'camp_educator_camp_period_id_fkey', 'fk_camp_educator_camp_period_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_educator', 'educator_id', 'educators.educator', 'camp_educator_educator_id_fkey', 'fk_camp_educator_educator_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('camps.camp_enrollment', 'camp_period_id', 'camps.camp_period', 'camp_enrollment_camp_period_id_fkey', 'fk_camp_enrollment_camp_period_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_enrollment', 'student_id', 'students.student', 'camp_enrollment_student_id_fkey', 'fk_camp_enrollment_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('camps.camp_enrollment', 'student_package_id', 'finance.student_package', 'camp_enrollment_student_package_id_fkey', 'fk_camp_enrollment_student_package_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('camps.camp_period', 'camp_id', 'camps.camp', 'camp_period_camp_id_fkey', 'fk_camp_period_camp_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_report', 'camp_enrollment_id', 'camps.camp_enrollment', 'camp_report_camp_enrollment_id_fkey', 'fk_camp_report_camp_enrollment_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('camps.camp_report', 'file_id', 'core.file_object', 'camp_report_file_id_fkey', 'fk_camp_report_file_id_tnt', 'a', 'NO ACTION', false);

-- ── consultancy
select pg_temp.to_tenant_fk('consultancy.consultancy_agreement', 'consultancy_plan_id', 'consultancy.consultancy_plan', 'consultancy_agreement_consultancy_plan_id_fkey', 'fk_consultancy_agreement_consultancy_plan_id_tnt', 'r', 'RESTRICT', false);
select pg_temp.to_tenant_fk('consultancy.consultancy_agreement', 'file_id', 'core.file_object', 'consultancy_agreement_file_id_fkey', 'fk_consultancy_agreement_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('consultancy.consultancy_agreement', 'institution_id', 'consultancy.institution', 'consultancy_agreement_institution_id_fkey', 'fk_consultancy_agreement_institution_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('consultancy.consultancy_plan', 'institution_id', 'consultancy.institution', 'consultancy_plan_institution_id_fkey', 'fk_consultancy_plan_institution_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('consultancy.consultancy_plan', 'lead_educator_id', 'educators.educator', 'consultancy_plan_lead_educator_id_fkey', 'fk_consultancy_plan_lead_educator_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('consultancy.consultancy_report', 'consultancy_plan_id', 'consultancy.consultancy_plan', 'consultancy_report_consultancy_plan_id_fkey', 'fk_consultancy_report_consultancy_plan_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('consultancy.consultancy_report', 'file_id', 'core.file_object', 'consultancy_report_file_id_fkey', 'fk_consultancy_report_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('consultancy.consultancy_report', 'school_visit_id', 'consultancy.school_visit', 'consultancy_report_school_visit_id_fkey', 'fk_consultancy_report_school_visit_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('consultancy.follow_up_activity', 'assigned_to', 'educators.educator', 'follow_up_activity_assigned_to_fkey', 'fk_follow_up_activity_assigned_to_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('consultancy.follow_up_activity', 'consultancy_plan_id', 'consultancy.consultancy_plan', 'follow_up_activity_consultancy_plan_id_fkey', 'fk_follow_up_activity_consultancy_plan_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('consultancy.follow_up_activity', 'observation_record_id', 'consultancy.observation_record', 'follow_up_activity_observation_record_id_fkey', 'fk_follow_up_activity_observation_record_id_tnt', 'n', 'SET NULL (observation_record_id)', false);
select pg_temp.to_tenant_fk('consultancy.follow_up_activity', 'school_visit_id', 'consultancy.school_visit', 'follow_up_activity_school_visit_id_fkey', 'fk_follow_up_activity_school_visit_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('consultancy.observation_record', 'school_visit_id', 'consultancy.school_visit', 'observation_record_school_visit_id_fkey', 'fk_observation_record_school_visit_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('consultancy.school_visit', 'consultancy_plan_id', 'consultancy.consultancy_plan', 'school_visit_consultancy_plan_id_fkey', 'fk_school_visit_consultancy_plan_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('consultancy.school_visit', 'institution_id', 'consultancy.institution', 'school_visit_institution_id_fkey', 'fk_school_visit_institution_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('consultancy.school_visit', 'visitor_id', 'educators.educator', 'school_visit_visitor_id_fkey', 'fk_school_visit_visitor_id_tnt', 'a', 'NO ACTION', false);

-- ── core
select pg_temp.to_tenant_fk('core.file_attachment', 'file_id', 'core.file_object', 'file_attachment_file_id_fkey', 'fk_file_attachment_file_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('core.notification', 'recipient_user_id', 'iam.user_account', 'notification_recipient_user_id_fkey', 'fk_notification_recipient_user_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('core.notification_preference', 'user_id', 'iam.user_account', 'notification_preference_user_id_fkey', 'fk_notification_preference_user_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('core.report_run', 'result_file_id', 'core.file_object', 'report_run_result_file_id_fkey', 'fk_report_run_result_file_id_tnt', 'a', 'NO ACTION', false);

-- ── crm
select pg_temp.to_tenant_fk('crm.interview', 'campus_id', 'core.campus', 'interview_campus_id_fkey', 'fk_interview_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('crm.interview', 'conducted_by', 'iam.user_account', 'interview_conducted_by_fkey', 'fk_interview_conducted_by_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('crm.interview', 'lead_id', 'crm.lead', 'interview_lead_id_fkey', 'fk_interview_lead_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('crm.lead', 'assigned_to', 'iam.user_account', 'lead_assigned_to_fkey', 'fk_lead_assigned_to_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('crm.lead', 'campus_id', 'core.campus', 'lead_campus_id_fkey', 'fk_lead_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('crm.lead', 'converted_student_id', 'students.student', 'lead_converted_student_id_fkey', 'fk_lead_converted_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('crm.lead_activity', 'lead_id', 'crm.lead', 'lead_activity_lead_id_fkey', 'fk_lead_activity_lead_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('crm.lead_activity', 'performed_by', 'iam.user_account', 'lead_activity_performed_by_fkey', 'fk_lead_activity_performed_by_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('crm.lead_status_history', 'lead_id', 'crm.lead', 'lead_status_history_lead_id_fkey', 'fk_lead_status_history_lead_id_tnt', 'c', 'CASCADE', false);

-- ── education
select pg_temp.to_tenant_fk('education.education_plan', 'academic_period_id', 'education.academic_period', 'education_plan_academic_period_id_fkey', 'fk_education_plan_academic_period_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.education_plan', 'approved_by', 'educators.educator', 'education_plan_approved_by_fkey', 'fk_education_plan_approved_by_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.education_plan', 'campus_id', 'core.campus', 'education_plan_campus_id_fkey', 'fk_education_plan_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.education_plan', 'prepared_by', 'educators.educator', 'education_plan_prepared_by_fkey', 'fk_education_plan_prepared_by_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.education_plan', 'student_id', 'students.student', 'education_plan_student_id_fkey', 'fk_education_plan_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('education.education_plan_approval', 'approver_id', 'educators.educator', 'education_plan_approval_approver_id_fkey', 'fk_education_plan_approval_approver_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.education_plan_approval', 'education_plan_id', 'education.education_plan', 'education_plan_approval_education_plan_id_fkey', 'fk_education_plan_approval_education_plan_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('education.education_plan_goal', 'education_plan_id', 'education.education_plan', 'education_plan_goal_education_plan_id_fkey', 'fk_education_plan_goal_education_plan_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('education.education_plan_goal', 'student_goal_id', 'education.student_goal', 'education_plan_goal_student_goal_id_fkey', 'fk_education_plan_goal_student_goal_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.education_plan_review', 'education_plan_id', 'education.education_plan', 'education_plan_review_education_plan_id_fkey', 'fk_education_plan_review_education_plan_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('education.education_plan_review', 'reviewer_id', 'educators.educator', 'education_plan_review_reviewer_id_fkey', 'fk_education_plan_review_reviewer_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.education_plan_revision', 'education_plan_id', 'education.education_plan', 'education_plan_revision_education_plan_id_fkey', 'fk_education_plan_revision_education_plan_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('education.education_plan_revision', 'revised_by', 'educators.educator', 'education_plan_revision_revised_by_fkey', 'fk_education_plan_revision_revised_by_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.enrollment', 'campus_id', 'core.campus', 'enrollment_campus_id_fkey', 'fk_enrollment_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.enrollment', 'student_id', 'students.student', 'enrollment_student_id_fkey', 'fk_enrollment_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('education.goal_progress', 'student_goal_id', 'education.student_goal', 'goal_progress_student_goal_id_fkey', 'fk_goal_progress_student_goal_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('education.program_service', 'program_id', 'education.program', 'program_service_program_id_fkey', 'fk_program_service_program_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('education.student_goal', 'parent_goal_id', 'education.student_goal', 'student_goal_parent_goal_id_fkey', 'fk_student_goal_parent_goal_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.student_goal', 'student_id', 'students.student', 'student_goal_student_id_fkey', 'fk_student_goal_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('education.student_program', 'campus_id', 'core.campus', 'student_program_campus_id_fkey', 'fk_student_program_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.student_program', 'enrollment_id', 'education.enrollment', 'student_program_enrollment_id_fkey', 'fk_student_program_enrollment_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.student_program', 'program_id', 'education.program', 'student_program_program_id_fkey', 'fk_student_program_program_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('education.student_program', 'student_id', 'students.student', 'student_program_student_id_fkey', 'fk_student_program_student_id_tnt', 'c', 'CASCADE', false);

-- ── educators
select pg_temp.to_tenant_fk('educators.educator', 'primary_campus_id', 'core.campus', 'educator_primary_campus_id_fkey', 'fk_educator_primary_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('educators.educator', 'user_id', 'iam.user_account', 'educator_user_id_fkey', 'fk_educator_user_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('educators.educator_campus', 'campus_id', 'core.campus', 'educator_campus_campus_id_fkey', 'fk_educator_campus_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('educators.educator_campus', 'educator_id', 'educators.educator', 'educator_campus_educator_id_fkey', 'fk_educator_campus_educator_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('educators.educator_certification', 'educator_id', 'educators.educator', 'educator_certification_educator_id_fkey', 'fk_educator_certification_educator_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('educators.educator_certification', 'file_id', 'core.file_object', 'educator_certification_file_id_fkey', 'fk_educator_certification_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('educators.educator_hierarchy', 'campus_id', 'core.campus', 'educator_hierarchy_campus_id_fkey', 'fk_educator_hierarchy_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('educators.educator_hierarchy', 'educator_id', 'educators.educator', 'educator_hierarchy_educator_id_fkey', 'fk_educator_hierarchy_educator_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('educators.educator_hierarchy', 'supervisor_id', 'educators.educator', 'educator_hierarchy_supervisor_id_fkey', 'fk_educator_hierarchy_supervisor_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('educators.educator_specialty', 'educator_id', 'educators.educator', 'educator_specialty_educator_id_fkey', 'fk_educator_specialty_educator_id_tnt', 'c', 'CASCADE', false);

-- ── finance
select pg_temp.to_tenant_fk('finance.credit_ledger', 'session_id', 'scheduling.session', 'credit_ledger_session_id_fkey', 'fk_credit_ledger_session_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.credit_ledger', 'student_package_id', 'finance.student_package', 'credit_ledger_student_package_id_fkey', 'fk_credit_ledger_student_package_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('finance.discount', 'invoice_id', 'finance.invoice', 'discount_invoice_id_fkey', 'fk_discount_invoice_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.discount', 'student_package_id', 'finance.student_package', 'discount_student_package_id_fkey', 'fk_discount_student_package_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.invoice', 'guardian_id', 'students.guardian', 'invoice_guardian_id_fkey', 'fk_invoice_guardian_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.invoice', 'student_id', 'students.student', 'invoice_student_id_fkey', 'fk_invoice_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.invoice_line', 'invoice_id', 'finance.invoice', 'invoice_line_invoice_id_fkey', 'fk_invoice_line_invoice_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('finance.invoice_line', 'student_package_id', 'finance.student_package', 'invoice_line_student_package_id_fkey', 'fk_invoice_line_student_package_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.package_definition', 'program_id', 'education.program', 'package_definition_program_id_fkey', 'fk_package_definition_program_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.payment', 'gateway_provider_id', 'core.integration_connection', 'payment_gateway_provider_id_fkey', 'fk_payment_gateway_provider_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.payment', 'invoice_id', 'finance.invoice', 'payment_invoice_id_fkey', 'fk_payment_invoice_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.payment', 'student_id', 'students.student', 'payment_student_id_fkey', 'fk_payment_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.refund', 'payment_id', 'finance.payment', 'refund_payment_id_fkey', 'fk_refund_payment_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.scholarship', 'student_id', 'students.student', 'scholarship_student_id_fkey', 'fk_scholarship_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.student_package', 'package_definition_id', 'finance.package_definition', 'student_package_package_definition_id_fkey', 'fk_student_package_package_definition_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('finance.student_package', 'student_id', 'students.student', 'student_package_student_id_fkey', 'fk_student_package_student_id_tnt', 'c', 'CASCADE', false);

-- ── iam
select pg_temp.to_tenant_fk('iam.auth_session', 'user_id', 'iam.user_account', 'auth_session_user_id_fkey', 'fk_auth_session_user_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('iam.user_account', 'primary_campus_id', 'core.campus', 'user_account_primary_campus_id_fkey', 'fk_user_account_primary_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('iam.user_role', 'campus_id', 'core.campus', 'user_role_campus_id_fkey', 'fk_user_role_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('iam.user_role', 'user_id', 'iam.user_account', 'user_role_user_id_fkey', 'fk_user_role_user_id_tnt', 'c', 'CASCADE', false);

-- ── legal
select pg_temp.to_tenant_fk('legal.student_consent', 'evidence_file_id', 'core.file_object', 'student_consent_evidence_file_id_fkey', 'fk_student_consent_evidence_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('legal.student_consent', 'guardian_id', 'students.guardian', 'student_consent_guardian_id_fkey', 'fk_student_consent_guardian_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('legal.student_consent', 'student_id', 'students.student', 'student_consent_student_id_fkey', 'fk_student_consent_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('legal.student_consent', 'template_id', 'legal.consent_template', 'student_consent_template_id_fkey', 'fk_student_consent_template_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('legal.student_contract', 'guardian_id', 'students.guardian', 'student_contract_guardian_id_fkey', 'fk_student_contract_guardian_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('legal.student_contract', 'signed_file_id', 'core.file_object', 'student_contract_signed_file_id_fkey', 'fk_student_contract_signed_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('legal.student_contract', 'student_id', 'students.student', 'student_contract_student_id_fkey', 'fk_student_contract_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('legal.student_contract', 'template_id', 'legal.contract_template', 'student_contract_template_id_fkey', 'fk_student_contract_template_id_tnt', 'a', 'NO ACTION', false);

-- ── media
select pg_temp.to_tenant_fk('media.camera', 'campus_id', 'core.campus', 'camera_campus_id_fkey', 'fk_camera_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('media.camera', 'stream_provider_id', 'core.integration_connection', 'camera_stream_provider_id_fkey', 'fk_camera_stream_provider_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('media.room_camera', 'camera_id', 'media.camera', 'room_camera_camera_id_fkey', 'fk_room_camera_camera_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('media.room_camera', 'room_id', 'scheduling.room', 'room_camera_room_id_fkey', 'fk_room_camera_room_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('media.session_camera', 'camera_id', 'media.camera', 'session_camera_camera_id_fkey', 'fk_session_camera_camera_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('media.session_camera', 'session_id', 'scheduling.session', 'session_camera_session_id_fkey', 'fk_session_camera_session_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('media.viewing_authorization', 'consent_id', 'legal.student_consent', 'viewing_authorization_consent_id_fkey', 'fk_viewing_authorization_consent_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('media.viewing_authorization', 'guardian_id', 'students.guardian', 'viewing_authorization_guardian_id_fkey', 'fk_viewing_authorization_guardian_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('media.viewing_authorization', 'session_id', 'scheduling.session', 'viewing_authorization_session_id_fkey', 'fk_viewing_authorization_session_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('media.viewing_authorization', 'student_id', 'students.student', 'viewing_authorization_student_id_fkey', 'fk_viewing_authorization_student_id_tnt', 'a', 'NO ACTION', false);

-- ── ops
select pg_temp.to_tenant_fk('ops.educator_performance_snapshot', 'educator_id', 'educators.educator', 'educator_performance_snapshot_educator_id_fkey', 'fk_educator_performance_snapshot_educator_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('ops.leave_approval', 'approver_id', 'iam.user_account', 'leave_approval_approver_id_fkey', 'fk_leave_approval_approver_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.leave_approval', 'leave_request_id', 'ops.leave_request', 'leave_approval_leave_request_id_fkey', 'fk_leave_approval_leave_request_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('ops.leave_balance', 'educator_id', 'educators.educator', 'leave_balance_educator_id_fkey', 'fk_leave_balance_educator_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('ops.leave_request', 'educator_id', 'educators.educator', 'leave_request_educator_id_fkey', 'fk_leave_request_educator_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('ops.meeting', 'campus_id', 'core.campus', 'meeting_campus_id_fkey', 'fk_meeting_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.meeting', 'organizer_id', 'iam.user_account', 'meeting_organizer_id_fkey', 'fk_meeting_organizer_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.meeting', 'room_id', 'scheduling.room', 'meeting_room_id_fkey', 'fk_meeting_room_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.meeting_follow_up', 'assignee_id', 'iam.user_account', 'meeting_follow_up_assignee_id_fkey', 'fk_meeting_follow_up_assignee_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.meeting_follow_up', 'meeting_id', 'ops.meeting', 'meeting_follow_up_meeting_id_fkey', 'fk_meeting_follow_up_meeting_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('ops.meeting_outcome', 'meeting_id', 'ops.meeting', 'meeting_outcome_meeting_id_fkey', 'fk_meeting_outcome_meeting_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('ops.meeting_participant', 'guardian_id', 'students.guardian', 'meeting_participant_guardian_id_fkey', 'fk_meeting_participant_guardian_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.meeting_participant', 'lead_id', 'crm.lead', 'meeting_participant_lead_id_fkey', 'fk_meeting_participant_lead_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.meeting_participant', 'meeting_id', 'ops.meeting', 'meeting_participant_meeting_id_fkey', 'fk_meeting_participant_meeting_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('ops.meeting_participant', 'user_id', 'iam.user_account', 'meeting_participant_user_id_fkey', 'fk_meeting_participant_user_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.parent_feedback', 'educator_id', 'educators.educator', 'parent_feedback_educator_id_fkey', 'fk_parent_feedback_educator_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.parent_feedback', 'guardian_id', 'students.guardian', 'parent_feedback_guardian_id_fkey', 'fk_parent_feedback_guardian_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.parent_feedback', 'session_id', 'scheduling.session', 'parent_feedback_session_id_fkey', 'fk_parent_feedback_session_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.survey_response', 'guardian_id', 'students.guardian', 'survey_response_guardian_id_fkey', 'fk_survey_response_guardian_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.survey_response', 'respondent_user_id', 'iam.user_account', 'survey_response_respondent_user_id_fkey', 'fk_survey_response_respondent_user_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.survey_response', 'session_id', 'scheduling.session', 'survey_response_session_id_fkey', 'fk_survey_response_session_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.survey_response', 'student_id', 'students.student', 'survey_response_student_id_fkey', 'fk_survey_response_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('ops.survey_response', 'survey_id', 'ops.survey_definition', 'survey_response_survey_id_fkey', 'fk_survey_response_survey_id_tnt', 'a', 'NO ACTION', false);

-- ── scheduling
select pg_temp.to_tenant_fk('scheduling.attendance', 'session_id', 'scheduling.session', 'attendance_session_id_fkey', 'fk_attendance_session_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('scheduling.attendance', 'student_id', 'students.student', 'attendance_student_id_fkey', 'fk_attendance_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.calendar_entry', 'campus_id', 'core.campus', 'calendar_entry_campus_id_fkey', 'fk_calendar_entry_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.makeup_request', 'makeup_session_id', 'scheduling.session', 'makeup_request_makeup_session_id_fkey', 'fk_makeup_request_makeup_session_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.makeup_request', 'missed_session_id', 'scheduling.session', 'makeup_request_missed_session_id_fkey', 'fk_makeup_request_missed_session_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.makeup_request', 'student_id', 'students.student', 'makeup_request_student_id_fkey', 'fk_makeup_request_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.recurrence_exception', 'recurring_schedule_id', 'scheduling.recurring_schedule', 'recurrence_exception_recurring_schedule_id_fkey', 'fk_recurrence_exception_recurring_schedule_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('scheduling.recurring_schedule', 'campus_id', 'core.campus', 'recurring_schedule_campus_id_fkey', 'fk_recurring_schedule_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.recurring_schedule', 'program_service_id', 'education.program_service', 'recurring_schedule_program_service_id_fkey', 'fk_recurring_schedule_program_service_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.recurring_schedule', 'room_id', 'scheduling.room', 'recurring_schedule_room_id_fkey', 'fk_recurring_schedule_room_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.recurring_schedule', 'student_program_id', 'education.student_program', 'recurring_schedule_student_program_id_fkey', 'fk_recurring_schedule_student_program_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.recurring_schedule_educator', 'educator_id', 'educators.educator', 'recurring_schedule_educator_educator_id_fkey', 'fk_recurring_schedule_educator_educator_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.recurring_schedule_educator', 'recurring_schedule_id', 'scheduling.recurring_schedule', 'recurring_schedule_educator_recurring_schedule_id_fkey', 'fk_recurring_schedule_educator_recurring_schedule_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('scheduling.room', 'campus_id', 'core.campus', 'room_campus_id_fkey', 'fk_room_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session', 'campus_id', 'core.campus', 'session_campus_id_fkey', 'fk_session_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session', 'program_service_id', 'education.program_service', 'session_program_service_id_fkey', 'fk_session_program_service_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session', 'recurring_schedule_id', 'scheduling.recurring_schedule', 'session_recurring_schedule_id_fkey', 'fk_session_recurring_schedule_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session', 'room_id', 'scheduling.room', 'session_room_id_fkey', 'fk_session_room_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session_educator', 'educator_id', 'educators.educator', 'session_educator_educator_id_fkey', 'fk_session_educator_educator_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session_educator', 'session_id', 'scheduling.session', 'session_educator_session_id_fkey', 'fk_session_educator_session_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('scheduling.session_goal', 'session_id', 'scheduling.session', 'session_goal_session_id_fkey', 'fk_session_goal_session_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('scheduling.session_goal', 'student_goal_id', 'education.student_goal', 'session_goal_student_goal_id_fkey', 'fk_session_goal_student_goal_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session_note', 'authored_by', 'educators.educator', 'session_note_authored_by_fkey', 'fk_session_note_authored_by_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session_note', 'student_id', 'students.student', 'session_note_student_id_fkey', 'fk_session_note_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session_note', 'session_id', 'scheduling.session', 'session_note_session_id_fkey', 'fk_session_note_session_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('scheduling.session_participant', 'session_id', 'scheduling.session', 'session_participant_session_id_fkey', 'fk_session_participant_session_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('scheduling.session_participant', 'student_id', 'students.student', 'session_participant_student_id_fkey', 'fk_session_participant_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('scheduling.session_participant', 'student_program_id', 'education.student_program', 'session_participant_student_program_id_fkey', 'fk_session_participant_student_program_id_tnt', 'a', 'NO ACTION', false);

-- ── students
select pg_temp.to_tenant_fk('students.case_note', 'student_id', 'students.student', 'case_note_student_id_fkey', 'fk_case_note_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.development_report', 'file_id', 'core.file_object', 'development_report_file_id_fkey', 'fk_development_report_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.development_report', 'student_id', 'students.student', 'development_report_student_id_fkey', 'fk_development_report_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.developmental_profile', 'student_id', 'students.student', 'developmental_profile_student_id_fkey', 'fk_developmental_profile_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.diagnosis', 'source_file_id', 'core.file_object', 'diagnosis_source_file_id_fkey', 'fk_diagnosis_source_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.diagnosis', 'student_id', 'students.student', 'diagnosis_student_id_fkey', 'fk_diagnosis_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.emergency_contact', 'student_id', 'students.student', 'emergency_contact_student_id_fkey', 'fk_emergency_contact_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.external_institution_report', 'file_id', 'core.file_object', 'external_institution_report_file_id_fkey', 'fk_external_institution_report_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.external_institution_report', 'student_id', 'students.student', 'external_institution_report_student_id_fkey', 'fk_external_institution_report_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.guardian', 'user_id', 'iam.user_account', 'guardian_user_id_fkey', 'fk_guardian_user_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.guardian_portal_access', 'guardian_id', 'students.guardian', 'guardian_portal_access_guardian_id_fkey', 'fk_guardian_portal_access_guardian_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.guardian_portal_access', 'student_id', 'students.student', 'guardian_portal_access_student_id_fkey', 'fk_guardian_portal_access_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.medical_report', 'file_id', 'core.file_object', 'medical_report_file_id_fkey', 'fk_medical_report_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.medical_report', 'student_id', 'students.student', 'medical_report_student_id_fkey', 'fk_medical_report_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.student', 'photo_file_id', 'core.file_object', 'student_photo_file_id_fkey', 'fk_student_photo_file_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.student', 'primary_campus_id', 'core.campus', 'student_primary_campus_id_fkey', 'fk_student_primary_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.student_campus', 'campus_id', 'core.campus', 'student_campus_campus_id_fkey', 'fk_student_campus_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.student_campus', 'student_id', 'students.student', 'student_campus_student_id_fkey', 'fk_student_campus_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.student_care_assignment', 'campus_id', 'core.campus', 'student_care_assignment_campus_id_fkey', 'fk_student_care_assignment_campus_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.student_care_assignment', 'educator_id', 'educators.educator', 'student_care_assignment_educator_id_fkey', 'fk_student_care_assignment_educator_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.student_care_assignment', 'granted_by', 'iam.user_account', 'student_care_assignment_granted_by_fkey', 'fk_student_care_assignment_granted_by_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.student_care_assignment', 'source_assignment_id', 'students.student_care_assignment', 'student_care_assignment_source_assignment_id_fkey', 'fk_student_care_assignment_source_assignment_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.student_care_assignment', 'student_id', 'students.student', 'student_care_assignment_student_id_fkey', 'fk_student_care_assignment_student_id_tnt', 'a', 'NO ACTION', false);
select pg_temp.to_tenant_fk('students.student_guardian', 'guardian_id', 'students.guardian', 'student_guardian_guardian_id_fkey', 'fk_student_guardian_guardian_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.student_guardian', 'student_id', 'students.student', 'student_guardian_student_id_fkey', 'fk_student_guardian_student_id_tnt', 'c', 'CASCADE', false);
select pg_temp.to_tenant_fk('students.student_status_history', 'student_id', 'students.student', 'student_status_history_student_id_fkey', 'fk_student_status_history_student_id_tnt', 'c', 'CASCADE', false);

-- ── Global-or-tenant references ──

-- Generic guard for references to tables that hold both global (corporation_id IS NULL)
-- and tenant rows: the referenced row must be global or belong to the same tenant.
-- TG_ARGV[0] = referenced table, TG_ARGV[1..] = referencing columns.
-- SECURITY DEFINER so the check sees every tenant's rows regardless of RLS.
create or replace function core.enforce_global_or_tenant_ref()
returns trigger
language plpgsql
security definer
set search_path = pg_catalog, pg_temp
as $fn$
declare
    v_row    jsonb := to_jsonb(new);
    v_corp   uuid  := (v_row ->> 'corporation_id')::uuid;
    v_target regclass := tg_argv[0]::regclass;
    v_col    text;
    v_id     uuid;
    v_ok     boolean;
begin
    for i in 1 .. tg_nargs - 1 loop
        v_col := tg_argv[i];
        v_id  := (v_row ->> v_col)::uuid;
        continue when v_id is null;
        execute format('select exists (select 1 from %s t where t.id = $1 '
                       'and (t.corporation_id is null or t.corporation_id is not distinct from $2))', v_target)
           into v_ok using v_id, v_corp;
        if not v_ok then
            raise exception using
                errcode    = '23503',
                message    = format('insert or update on table "%s" violates tenant scope of "%s"',
                                    tg_table_name, v_col),
                detail     = format('Key (%s)=(%s) is not present in table "%s" for this tenant.',
                                    v_col, v_id, v_target),
                schema     = tg_table_schema,
                table      = tg_table_name,
                column     = v_col,
                constraint = format('%s_%s_scope', tg_table_name, v_col);
        end if;
    end loop;
    return new;
end
$fn$;

comment on function core.enforce_global_or_tenant_ref() is
  'V41 (#86): references to tables with global rows must target a global row or a row of the same tenant.';

drop trigger if exists trg_assessment_session_scope_assessment_template on assessment.assessment_session;
create trigger trg_assessment_session_scope_assessment_template
    before insert or update of corporation_id, template_id on assessment.assessment_session
    for each row execute function core.enforce_global_or_tenant_ref('assessment.assessment_template', 'template_id');
drop trigger if exists trg_assessment_template_scope_ref_value on assessment.assessment_template;
create trigger trg_assessment_template_scope_ref_value
    before insert or update of corporation_id, category_id, type_id on assessment.assessment_template
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'category_id', 'type_id');
drop trigger if exists trg_camp_scope_ref_value on camps.camp;
create trigger trg_camp_scope_ref_value
    before insert or update of corporation_id, camp_type_id on camps.camp
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'camp_type_id');
drop trigger if exists trg_camp_activity_scope_ref_value on camps.camp_activity;
create trigger trg_camp_activity_scope_ref_value
    before insert or update of corporation_id, activity_type_id on camps.camp_activity
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'activity_type_id');
drop trigger if exists trg_camp_attendance_scope_ref_value on camps.camp_attendance;
create trigger trg_camp_attendance_scope_ref_value
    before insert or update of corporation_id, reason_id on camps.camp_attendance
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'reason_id');
drop trigger if exists trg_consultancy_agreement_scope_ref_value on consultancy.consultancy_agreement;
create trigger trg_consultancy_agreement_scope_ref_value
    before insert or update of corporation_id, agreement_type_id on consultancy.consultancy_agreement
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'agreement_type_id');
drop trigger if exists trg_consultancy_plan_scope_ref_value on consultancy.consultancy_plan;
create trigger trg_consultancy_plan_scope_ref_value
    before insert or update of corporation_id, consultancy_type_id on consultancy.consultancy_plan
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'consultancy_type_id');
drop trigger if exists trg_institution_scope_ref_value on consultancy.institution;
create trigger trg_institution_scope_ref_value
    before insert or update of corporation_id, institution_type_id on consultancy.institution
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'institution_type_id');
drop trigger if exists trg_observation_record_scope_ref_value on consultancy.observation_record;
create trigger trg_observation_record_scope_ref_value
    before insert or update of corporation_id, observation_type_id on consultancy.observation_record
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'observation_type_id');
drop trigger if exists trg_kpi_definition_scope_ref_value on core.kpi_definition;
create trigger trg_kpi_definition_scope_ref_value
    before insert or update of corporation_id, category_id on core.kpi_definition
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'category_id');
drop trigger if exists trg_kpi_value_scope_kpi_definition on core.kpi_value;
create trigger trg_kpi_value_scope_kpi_definition
    before insert or update of corporation_id, kpi_id on core.kpi_value
    for each row execute function core.enforce_global_or_tenant_ref('core.kpi_definition', 'kpi_id');
drop trigger if exists trg_notification_scope_notification_template on core.notification;
create trigger trg_notification_scope_notification_template
    before insert or update of corporation_id, template_id on core.notification
    for each row execute function core.enforce_global_or_tenant_ref('core.notification_template', 'template_id');
drop trigger if exists trg_notification_scope_ref_value on core.notification;
create trigger trg_notification_scope_ref_value
    before insert or update of corporation_id, category_id on core.notification
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'category_id');
drop trigger if exists trg_notification_preference_scope_ref_value on core.notification_preference;
create trigger trg_notification_preference_scope_ref_value
    before insert or update of corporation_id, category_id, channel_id on core.notification_preference
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'category_id', 'channel_id');
drop trigger if exists trg_notification_template_scope_ref_value on core.notification_template;
create trigger trg_notification_template_scope_ref_value
    before insert or update of corporation_id, category_id, type_id on core.notification_template
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'category_id', 'type_id');
drop trigger if exists trg_notification_trigger_config_scope_notification_template on core.notification_trigger_config;
create trigger trg_notification_trigger_config_scope_notification_template
    before insert or update of corporation_id, template_id on core.notification_trigger_config
    for each row execute function core.enforce_global_or_tenant_ref('core.notification_template', 'template_id');
drop trigger if exists trg_report_definition_scope_ref_value on core.report_definition;
create trigger trg_report_definition_scope_ref_value
    before insert or update of corporation_id, category_id on core.report_definition
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'category_id');
drop trigger if exists trg_report_run_scope_report_definition on core.report_run;
create trigger trg_report_run_scope_report_definition
    before insert or update of corporation_id, report_definition_id on core.report_run
    for each row execute function core.enforce_global_or_tenant_ref('core.report_definition', 'report_definition_id');
drop trigger if exists trg_report_schedule_scope_report_definition on core.report_schedule;
create trigger trg_report_schedule_scope_report_definition
    before insert or update of corporation_id, report_definition_id on core.report_schedule
    for each row execute function core.enforce_global_or_tenant_ref('core.report_definition', 'report_definition_id');
drop trigger if exists trg_lead_scope_ref_value on crm.lead;
create trigger trg_lead_scope_ref_value
    before insert or update of corporation_id, pipeline_stage_id, source_id, status_id on crm.lead
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'pipeline_stage_id', 'source_id', 'status_id');
drop trigger if exists trg_lead_activity_scope_ref_value on crm.lead_activity;
create trigger trg_lead_activity_scope_ref_value
    before insert or update of corporation_id, activity_type_id on crm.lead_activity
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'activity_type_id');
drop trigger if exists trg_lead_status_history_scope_ref_value on crm.lead_status_history;
create trigger trg_lead_status_history_scope_ref_value
    before insert or update of corporation_id, pipeline_stage_id, status_id on crm.lead_status_history
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'pipeline_stage_id', 'status_id');
drop trigger if exists trg_academic_period_scope_ref_value on education.academic_period;
create trigger trg_academic_period_scope_ref_value
    before insert or update of corporation_id, term_id on education.academic_period
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'term_id');
drop trigger if exists trg_enrollment_scope_ref_value on education.enrollment;
create trigger trg_enrollment_scope_ref_value
    before insert or update of corporation_id, status_id on education.enrollment
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'status_id');
drop trigger if exists trg_goal_template_scope_goal_library on education.goal_template;
create trigger trg_goal_template_scope_goal_library
    before insert or update of corporation_id, library_id on education.goal_template
    for each row execute function core.enforce_global_or_tenant_ref('education.goal_library', 'library_id');
drop trigger if exists trg_goal_template_scope_ref_value on education.goal_template;
create trigger trg_goal_template_scope_ref_value
    before insert or update of corporation_id, category_id, development_area_id on education.goal_template
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'category_id', 'development_area_id');
drop trigger if exists trg_program_scope_ref_value on education.program;
create trigger trg_program_scope_ref_value
    before insert or update of corporation_id, program_type_id on education.program
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'program_type_id');
drop trigger if exists trg_program_service_scope_ref_value on education.program_service;
create trigger trg_program_service_scope_ref_value
    before insert or update of corporation_id, service_type_id on education.program_service
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'service_type_id');
drop trigger if exists trg_student_goal_scope_goal_template on education.student_goal;
create trigger trg_student_goal_scope_goal_template
    before insert or update of corporation_id, template_id on education.student_goal
    for each row execute function core.enforce_global_or_tenant_ref('education.goal_template', 'template_id');
drop trigger if exists trg_student_goal_scope_ref_value on education.student_goal;
create trigger trg_student_goal_scope_ref_value
    before insert or update of corporation_id, category_id, development_area_id on education.student_goal
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'category_id', 'development_area_id');
drop trigger if exists trg_educator_scope_ref_value on educators.educator;
create trigger trg_educator_scope_ref_value
    before insert or update of corporation_id, title_id on educators.educator
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'title_id');
drop trigger if exists trg_educator_certification_scope_ref_value on educators.educator_certification;
create trigger trg_educator_certification_scope_ref_value
    before insert or update of corporation_id, certification_type_id on educators.educator_certification
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'certification_type_id');
drop trigger if exists trg_educator_hierarchy_scope_ref_value on educators.educator_hierarchy;
create trigger trg_educator_hierarchy_scope_ref_value
    before insert or update of corporation_id, relationship_id on educators.educator_hierarchy
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'relationship_id');
drop trigger if exists trg_educator_specialty_scope_ref_value on educators.educator_specialty;
create trigger trg_educator_specialty_scope_ref_value
    before insert or update of corporation_id, specialty_id on educators.educator_specialty
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'specialty_id');
drop trigger if exists trg_discount_scope_ref_value on finance.discount;
create trigger trg_discount_scope_ref_value
    before insert or update of corporation_id, discount_type_id on finance.discount
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'discount_type_id');
drop trigger if exists trg_package_definition_scope_ref_value on finance.package_definition;
create trigger trg_package_definition_scope_ref_value
    before insert or update of corporation_id, package_type_id on finance.package_definition
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'package_type_id');
drop trigger if exists trg_payment_scope_ref_value on finance.payment;
create trigger trg_payment_scope_ref_value
    before insert or update of corporation_id, payment_method_id on finance.payment
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'payment_method_id');
drop trigger if exists trg_scholarship_scope_ref_value on finance.scholarship;
create trigger trg_scholarship_scope_ref_value
    before insert or update of corporation_id, scholarship_type_id on finance.scholarship
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'scholarship_type_id');
drop trigger if exists trg_menu_item_scope_menu_item on iam.menu_item;
create trigger trg_menu_item_scope_menu_item
    before insert or update of corporation_id, parent_id on iam.menu_item
    for each row execute function core.enforce_global_or_tenant_ref('iam.menu_item', 'parent_id');
drop trigger if exists trg_user_role_scope_role on iam.user_role;
create trigger trg_user_role_scope_role
    before insert or update of corporation_id, role_id on iam.user_role
    for each row execute function core.enforce_global_or_tenant_ref('iam.role', 'role_id');
drop trigger if exists trg_consent_template_scope_ref_value on legal.consent_template;
create trigger trg_consent_template_scope_ref_value
    before insert or update of corporation_id, consent_type_id on legal.consent_template
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'consent_type_id');
drop trigger if exists trg_contract_template_scope_ref_value on legal.contract_template;
create trigger trg_contract_template_scope_ref_value
    before insert or update of corporation_id, contract_type_id on legal.contract_template
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'contract_type_id');
drop trigger if exists trg_student_consent_scope_ref_value on legal.student_consent;
create trigger trg_student_consent_scope_ref_value
    before insert or update of corporation_id, consent_type_id on legal.student_consent
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'consent_type_id');
drop trigger if exists trg_camera_scope_ref_value on media.camera;
create trigger trg_camera_scope_ref_value
    before insert or update of corporation_id, camera_type_id on media.camera
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'camera_type_id');
drop trigger if exists trg_viewing_authorization_scope_ref_value on media.viewing_authorization;
create trigger trg_viewing_authorization_scope_ref_value
    before insert or update of corporation_id, access_type_id on media.viewing_authorization
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'access_type_id');
drop trigger if exists trg_leave_balance_scope_ref_value on ops.leave_balance;
create trigger trg_leave_balance_scope_ref_value
    before insert or update of corporation_id, leave_type_id on ops.leave_balance
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'leave_type_id');
drop trigger if exists trg_leave_request_scope_ref_value on ops.leave_request;
create trigger trg_leave_request_scope_ref_value
    before insert or update of corporation_id, leave_type_id on ops.leave_request
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'leave_type_id');
drop trigger if exists trg_meeting_scope_ref_value on ops.meeting;
create trigger trg_meeting_scope_ref_value
    before insert or update of corporation_id, meeting_type_id on ops.meeting
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'meeting_type_id');
drop trigger if exists trg_survey_definition_scope_ref_value on ops.survey_definition;
create trigger trg_survey_definition_scope_ref_value
    before insert or update of corporation_id, type_id on ops.survey_definition
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'type_id');
drop trigger if exists trg_ref_value_scope_ref_value on ref.ref_value;
create trigger trg_ref_value_scope_ref_value
    before insert or update of corporation_id, parent_value_id on ref.ref_value
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'parent_value_id');
drop trigger if exists trg_ref_value_tenant_override_scope_ref_value on ref.ref_value_tenant_override;
create trigger trg_ref_value_tenant_override_scope_ref_value
    before insert or update of corporation_id, ref_value_id on ref.ref_value_tenant_override
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'ref_value_id');
drop trigger if exists trg_attendance_scope_ref_value on scheduling.attendance;
create trigger trg_attendance_scope_ref_value
    before insert or update of corporation_id, reason_id on scheduling.attendance
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'reason_id');
drop trigger if exists trg_makeup_request_scope_ref_value on scheduling.makeup_request;
create trigger trg_makeup_request_scope_ref_value
    before insert or update of corporation_id, missed_reason_id on scheduling.makeup_request
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'missed_reason_id');
drop trigger if exists trg_recurring_schedule_scope_ref_value on scheduling.recurring_schedule;
create trigger trg_recurring_schedule_scope_ref_value
    before insert or update of corporation_id, session_type_id on scheduling.recurring_schedule
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'session_type_id');
drop trigger if exists trg_room_scope_ref_value on scheduling.room;
create trigger trg_room_scope_ref_value
    before insert or update of corporation_id, room_type_id on scheduling.room
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'room_type_id');
drop trigger if exists trg_session_scope_ref_value on scheduling.session;
create trigger trg_session_scope_ref_value
    before insert or update of corporation_id, session_type_id on scheduling.session
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'session_type_id');
drop trigger if exists trg_developmental_profile_scope_ref_value on students.developmental_profile;
create trigger trg_developmental_profile_scope_ref_value
    before insert or update of corporation_id, development_area_id on students.developmental_profile
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'development_area_id');
drop trigger if exists trg_diagnosis_scope_ref_value on students.diagnosis;
create trigger trg_diagnosis_scope_ref_value
    before insert or update of corporation_id, category_id on students.diagnosis
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'category_id');
drop trigger if exists trg_external_institution_report_scope_ref_value on students.external_institution_report;
create trigger trg_external_institution_report_scope_ref_value
    before insert or update of corporation_id, institution_type_id on students.external_institution_report
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'institution_type_id');
drop trigger if exists trg_student_scope_ref_value on students.student;
create trigger trg_student_scope_ref_value
    before insert or update of corporation_id, status_id on students.student
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'status_id');
drop trigger if exists trg_student_care_assignment_scope_ref_value on students.student_care_assignment;
create trigger trg_student_care_assignment_scope_ref_value
    before insert or update of corporation_id, grant_type_id, role_id on students.student_care_assignment
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'grant_type_id', 'role_id');
drop trigger if exists trg_student_guardian_scope_ref_value on students.student_guardian;
create trigger trg_student_guardian_scope_ref_value
    before insert or update of corporation_id, relationship_id on students.student_guardian
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'relationship_id');
drop trigger if exists trg_student_status_history_scope_ref_value on students.student_status_history;
create trigger trg_student_status_history_scope_ref_value
    before insert or update of corporation_id, status_id on students.student_status_history
    for each row execute function core.enforce_global_or_tenant_ref('ref.ref_value', 'status_id');
