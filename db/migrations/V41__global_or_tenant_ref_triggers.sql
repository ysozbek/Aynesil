-- =============================================================================
-- AyNesil :: V41 — Global-or-tenant reference triggers (#86, step 4/4)
-- 72 foreign keys point at tables that mix global rows (corporation_id IS NULL) and
-- tenant rows (ref.ref_value, templates, KPI/notification definitions, roles, menu items).
-- A composite FK cannot express "global OR same tenant", so a generic BEFORE trigger enforces it
-- (63 triggers). Violations raise SQLSTATE 23503 like a regular FK violation.
-- A row whose own corporation_id is NULL (a global row) may only reference global rows.
-- =============================================================================

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
