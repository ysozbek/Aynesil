-- =============================================================================
-- AyNesil :: V38 — Tenant FK unique keys (#86, step 1/4)
-- Adds a UNIQUE (corporation_id, id) index to the 39 tables referenced by
-- tenant-scoped composite foreign keys (V39). A non-partial unique index is a valid FK target,
-- so no constraint is attached (that would take an ACCESS EXCLUSIVE lock).
--
-- Runs outside a transaction (flyway.mixed=true + CREATE INDEX CONCURRENTLY): writes are not
-- blocked. If a previous attempt was interrupted, the leftover INVALID index is dropped first
-- so the file can simply be re-run.
-- Requires PostgreSQL 15+ for the whole V38–V41 set (see db/README.md).
-- Rollback: db/rollback/V38-V41__tenant_integrity_down.sql
-- =============================================================================

-- Drop INVALID leftovers of an interrupted run (only the indexes this migration owns).
do $cleanup$
declare r record;
begin
    for r in
        select n.nspname, c.relname
          from pg_index i
          join pg_class c on c.oid = i.indexrelid
          join pg_namespace n on n.oid = c.relnamespace
         where not i.indisvalid and c.relname like 'uq\_%\_corp\_id%'
    loop
        execute format('drop index if exists %I.%I', r.nspname, r.relname);
    end loop;
end
$cleanup$;

-- assessment
create unique index concurrently if not exists uq_assessment_session_corp_id on assessment.assessment_session (corporation_id, id);

-- camps
create unique index concurrently if not exists uq_camp_corp_id on camps.camp (corporation_id, id);
create unique index concurrently if not exists uq_camp_activity_corp_id on camps.camp_activity (corporation_id, id);
create unique index concurrently if not exists uq_camp_enrollment_corp_id on camps.camp_enrollment (corporation_id, id);
create unique index concurrently if not exists uq_camp_period_corp_id on camps.camp_period (corporation_id, id);

-- consultancy
create unique index concurrently if not exists uq_consultancy_plan_corp_id on consultancy.consultancy_plan (corporation_id, id);
create unique index concurrently if not exists uq_institution_corp_id on consultancy.institution (corporation_id, id);
create unique index concurrently if not exists uq_observation_record_corp_id on consultancy.observation_record (corporation_id, id);
create unique index concurrently if not exists uq_school_visit_corp_id on consultancy.school_visit (corporation_id, id);

-- core
create unique index concurrently if not exists uq_campus_corp_id on core.campus (corporation_id, id);
create unique index concurrently if not exists uq_file_object_corp_id on core.file_object (corporation_id, id);
create unique index concurrently if not exists uq_integration_connection_corp_id on core.integration_connection (corporation_id, id);

-- crm
create unique index concurrently if not exists uq_lead_corp_id on crm.lead (corporation_id, id);

-- education
create unique index concurrently if not exists uq_academic_period_corp_id on education.academic_period (corporation_id, id);
create unique index concurrently if not exists uq_education_plan_corp_id on education.education_plan (corporation_id, id);
create unique index concurrently if not exists uq_enrollment_corp_id on education.enrollment (corporation_id, id);
create unique index concurrently if not exists uq_program_corp_id on education.program (corporation_id, id);
create unique index concurrently if not exists uq_program_service_corp_id on education.program_service (corporation_id, id);
create unique index concurrently if not exists uq_student_goal_corp_id on education.student_goal (corporation_id, id);
create unique index concurrently if not exists uq_student_program_corp_id on education.student_program (corporation_id, id);

-- educators
create unique index concurrently if not exists uq_educator_corp_id on educators.educator (corporation_id, id);

-- finance
create unique index concurrently if not exists uq_invoice_corp_id on finance.invoice (corporation_id, id);
create unique index concurrently if not exists uq_package_definition_corp_id on finance.package_definition (corporation_id, id);
create unique index concurrently if not exists uq_payment_corp_id on finance.payment (corporation_id, id);
create unique index concurrently if not exists uq_student_package_corp_id on finance.student_package (corporation_id, id);

-- iam
create unique index concurrently if not exists uq_user_account_corp_id on iam.user_account (corporation_id, id);

-- legal
create unique index concurrently if not exists uq_consent_template_corp_id on legal.consent_template (corporation_id, id);
create unique index concurrently if not exists uq_contract_template_corp_id on legal.contract_template (corporation_id, id);
create unique index concurrently if not exists uq_student_consent_corp_id on legal.student_consent (corporation_id, id);

-- media
create unique index concurrently if not exists uq_camera_corp_id on media.camera (corporation_id, id);

-- ops
create unique index concurrently if not exists uq_leave_request_corp_id on ops.leave_request (corporation_id, id);
create unique index concurrently if not exists uq_meeting_corp_id on ops.meeting (corporation_id, id);
create unique index concurrently if not exists uq_survey_definition_corp_id on ops.survey_definition (corporation_id, id);

-- scheduling
create unique index concurrently if not exists uq_recurring_schedule_corp_id on scheduling.recurring_schedule (corporation_id, id);
create unique index concurrently if not exists uq_room_corp_id on scheduling.room (corporation_id, id);
create unique index concurrently if not exists uq_session_corp_id on scheduling.session (corporation_id, id);

-- students
create unique index concurrently if not exists uq_guardian_corp_id on students.guardian (corporation_id, id);
create unique index concurrently if not exists uq_student_corp_id on students.student (corporation_id, id);
create unique index concurrently if not exists uq_student_care_assignment_corp_id on students.student_care_assignment (corporation_id, id);
