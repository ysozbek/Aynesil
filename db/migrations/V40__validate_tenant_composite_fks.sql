-- =============================================================================
-- AyNesil :: V40 — Validate tenant composite foreign keys (#86, step 3/4)
-- Validates the 202 NOT VALID constraints added in V39. VALIDATE scans the referencing
-- table under SHARE UPDATE EXCLUSIVE, which does not block reads or writes.
-- Idempotent: validating an already valid constraint is a no-op.
-- =============================================================================

-- ── assessment
alter table assessment.assessment_report validate constraint fk_assessment_report_assessment_session_id_tnt;
alter table assessment.assessment_report validate constraint fk_assessment_report_file_id_tnt;
alter table assessment.assessment_session validate constraint fk_assessment_session_assessor_id_tnt;
alter table assessment.assessment_session validate constraint fk_assessment_session_campus_id_tnt;
alter table assessment.assessment_session validate constraint fk_assessment_session_lead_id_tnt;
alter table assessment.assessment_session validate constraint fk_assessment_session_student_id_tnt;
alter table assessment.program_recommendation validate constraint fk_program_recommendation_assessment_session_id_tnt;
alter table assessment.program_recommendation validate constraint fk_program_recommendation_lead_id_tnt;
alter table assessment.program_recommendation validate constraint fk_program_recommendation_recommended_by_tnt;
alter table assessment.program_recommendation validate constraint fk_program_recommendation_student_id_tnt;

-- ── camps
alter table camps.camp validate constraint fk_camp_campus_id_tnt;
alter table camps.camp_activity validate constraint fk_camp_activity_camp_period_id_tnt;
alter table camps.camp_activity validate constraint fk_camp_activity_session_id_tnt;
alter table camps.camp_activity_participation validate constraint fk_camp_activity_participation_camp_activity_id_tnt;
alter table camps.camp_activity_participation validate constraint fk_camp_activity_participation_camp_enrollment_id_tnt;
alter table camps.camp_attendance validate constraint fk_camp_attendance_camp_enrollment_id_tnt;
alter table camps.camp_educator validate constraint fk_camp_educator_camp_activity_id_tnt;
alter table camps.camp_educator validate constraint fk_camp_educator_camp_id_tnt;
alter table camps.camp_educator validate constraint fk_camp_educator_camp_period_id_tnt;
alter table camps.camp_educator validate constraint fk_camp_educator_educator_id_tnt;
alter table camps.camp_enrollment validate constraint fk_camp_enrollment_camp_period_id_tnt;
alter table camps.camp_enrollment validate constraint fk_camp_enrollment_student_id_tnt;
alter table camps.camp_enrollment validate constraint fk_camp_enrollment_student_package_id_tnt;
alter table camps.camp_period validate constraint fk_camp_period_camp_id_tnt;
alter table camps.camp_report validate constraint fk_camp_report_camp_enrollment_id_tnt;
alter table camps.camp_report validate constraint fk_camp_report_file_id_tnt;

-- ── consultancy
alter table consultancy.consultancy_agreement validate constraint fk_consultancy_agreement_consultancy_plan_id_tnt;
alter table consultancy.consultancy_agreement validate constraint fk_consultancy_agreement_file_id_tnt;
alter table consultancy.consultancy_agreement validate constraint fk_consultancy_agreement_institution_id_tnt;
alter table consultancy.consultancy_plan validate constraint fk_consultancy_plan_institution_id_tnt;
alter table consultancy.consultancy_plan validate constraint fk_consultancy_plan_lead_educator_id_tnt;
alter table consultancy.consultancy_report validate constraint fk_consultancy_report_consultancy_plan_id_tnt;
alter table consultancy.consultancy_report validate constraint fk_consultancy_report_file_id_tnt;
alter table consultancy.consultancy_report validate constraint fk_consultancy_report_school_visit_id_tnt;
alter table consultancy.follow_up_activity validate constraint fk_follow_up_activity_assigned_to_tnt;
alter table consultancy.follow_up_activity validate constraint fk_follow_up_activity_consultancy_plan_id_tnt;
alter table consultancy.follow_up_activity validate constraint fk_follow_up_activity_observation_record_id_tnt;
alter table consultancy.follow_up_activity validate constraint fk_follow_up_activity_school_visit_id_tnt;
alter table consultancy.observation_record validate constraint fk_observation_record_school_visit_id_tnt;
alter table consultancy.school_visit validate constraint fk_school_visit_consultancy_plan_id_tnt;
alter table consultancy.school_visit validate constraint fk_school_visit_institution_id_tnt;
alter table consultancy.school_visit validate constraint fk_school_visit_visitor_id_tnt;

-- ── core
alter table core.file_attachment validate constraint fk_file_attachment_file_id_tnt;
alter table core.notification validate constraint fk_notification_recipient_user_id_tnt;
alter table core.notification_preference validate constraint fk_notification_preference_user_id_tnt;
alter table core.report_run validate constraint fk_report_run_result_file_id_tnt;

-- ── crm
alter table crm.interview validate constraint fk_interview_campus_id_tnt;
alter table crm.interview validate constraint fk_interview_conducted_by_tnt;
alter table crm.interview validate constraint fk_interview_lead_id_tnt;
alter table crm.lead validate constraint fk_lead_assigned_to_tnt;
alter table crm.lead validate constraint fk_lead_campus_id_tnt;
alter table crm.lead validate constraint fk_lead_converted_student_id_tnt;
alter table crm.lead_activity validate constraint fk_lead_activity_lead_id_tnt;
alter table crm.lead_activity validate constraint fk_lead_activity_performed_by_tnt;
alter table crm.lead_status_history validate constraint fk_lead_status_history_lead_id_tnt;

-- ── education
alter table education.education_plan validate constraint fk_education_plan_academic_period_id_tnt;
alter table education.education_plan validate constraint fk_education_plan_approved_by_tnt;
alter table education.education_plan validate constraint fk_education_plan_campus_id_tnt;
alter table education.education_plan validate constraint fk_education_plan_prepared_by_tnt;
alter table education.education_plan validate constraint fk_education_plan_student_id_tnt;
alter table education.education_plan_approval validate constraint fk_education_plan_approval_approver_id_tnt;
alter table education.education_plan_approval validate constraint fk_education_plan_approval_education_plan_id_tnt;
alter table education.education_plan_goal validate constraint fk_education_plan_goal_education_plan_id_tnt;
alter table education.education_plan_goal validate constraint fk_education_plan_goal_student_goal_id_tnt;
alter table education.education_plan_review validate constraint fk_education_plan_review_education_plan_id_tnt;
alter table education.education_plan_review validate constraint fk_education_plan_review_reviewer_id_tnt;
alter table education.education_plan_revision validate constraint fk_education_plan_revision_education_plan_id_tnt;
alter table education.education_plan_revision validate constraint fk_education_plan_revision_revised_by_tnt;
alter table education.enrollment validate constraint fk_enrollment_campus_id_tnt;
alter table education.enrollment validate constraint fk_enrollment_student_id_tnt;
alter table education.goal_progress validate constraint fk_goal_progress_student_goal_id_tnt;
alter table education.program_service validate constraint fk_program_service_program_id_tnt;
alter table education.student_goal validate constraint fk_student_goal_parent_goal_id_tnt;
alter table education.student_goal validate constraint fk_student_goal_student_id_tnt;
alter table education.student_program validate constraint fk_student_program_campus_id_tnt;
alter table education.student_program validate constraint fk_student_program_enrollment_id_tnt;
alter table education.student_program validate constraint fk_student_program_program_id_tnt;
alter table education.student_program validate constraint fk_student_program_student_id_tnt;

-- ── educators
alter table educators.educator validate constraint fk_educator_primary_campus_id_tnt;
alter table educators.educator validate constraint fk_educator_user_id_tnt;
alter table educators.educator_campus validate constraint fk_educator_campus_campus_id_tnt;
alter table educators.educator_campus validate constraint fk_educator_campus_educator_id_tnt;
alter table educators.educator_certification validate constraint fk_educator_certification_educator_id_tnt;
alter table educators.educator_certification validate constraint fk_educator_certification_file_id_tnt;
alter table educators.educator_hierarchy validate constraint fk_educator_hierarchy_campus_id_tnt;
alter table educators.educator_hierarchy validate constraint fk_educator_hierarchy_educator_id_tnt;
alter table educators.educator_hierarchy validate constraint fk_educator_hierarchy_supervisor_id_tnt;
alter table educators.educator_specialty validate constraint fk_educator_specialty_educator_id_tnt;

-- ── finance
alter table finance.credit_ledger validate constraint fk_credit_ledger_session_id_tnt;
alter table finance.credit_ledger validate constraint fk_credit_ledger_student_package_id_tnt;
alter table finance.discount validate constraint fk_discount_invoice_id_tnt;
alter table finance.discount validate constraint fk_discount_student_package_id_tnt;
alter table finance.invoice validate constraint fk_invoice_guardian_id_tnt;
alter table finance.invoice validate constraint fk_invoice_student_id_tnt;
alter table finance.invoice_line validate constraint fk_invoice_line_invoice_id_tnt;
alter table finance.invoice_line validate constraint fk_invoice_line_student_package_id_tnt;
alter table finance.package_definition validate constraint fk_package_definition_program_id_tnt;
alter table finance.payment validate constraint fk_payment_gateway_provider_id_tnt;
alter table finance.payment validate constraint fk_payment_invoice_id_tnt;
alter table finance.payment validate constraint fk_payment_student_id_tnt;
alter table finance.refund validate constraint fk_refund_payment_id_tnt;
alter table finance.scholarship validate constraint fk_scholarship_student_id_tnt;
alter table finance.student_package validate constraint fk_student_package_package_definition_id_tnt;
alter table finance.student_package validate constraint fk_student_package_student_id_tnt;

-- ── iam
alter table iam.auth_session validate constraint fk_auth_session_user_id_tnt;
alter table iam.user_account validate constraint fk_user_account_primary_campus_id_tnt;
alter table iam.user_role validate constraint fk_user_role_campus_id_tnt;
alter table iam.user_role validate constraint fk_user_role_user_id_tnt;

-- ── legal
alter table legal.student_consent validate constraint fk_student_consent_evidence_file_id_tnt;
alter table legal.student_consent validate constraint fk_student_consent_guardian_id_tnt;
alter table legal.student_consent validate constraint fk_student_consent_student_id_tnt;
alter table legal.student_consent validate constraint fk_student_consent_template_id_tnt;
alter table legal.student_contract validate constraint fk_student_contract_guardian_id_tnt;
alter table legal.student_contract validate constraint fk_student_contract_signed_file_id_tnt;
alter table legal.student_contract validate constraint fk_student_contract_student_id_tnt;
alter table legal.student_contract validate constraint fk_student_contract_template_id_tnt;

-- ── media
alter table media.camera validate constraint fk_camera_campus_id_tnt;
alter table media.camera validate constraint fk_camera_stream_provider_id_tnt;
alter table media.room_camera validate constraint fk_room_camera_camera_id_tnt;
alter table media.room_camera validate constraint fk_room_camera_room_id_tnt;
alter table media.session_camera validate constraint fk_session_camera_camera_id_tnt;
alter table media.session_camera validate constraint fk_session_camera_session_id_tnt;
alter table media.viewing_authorization validate constraint fk_viewing_authorization_consent_id_tnt;
alter table media.viewing_authorization validate constraint fk_viewing_authorization_guardian_id_tnt;
alter table media.viewing_authorization validate constraint fk_viewing_authorization_session_id_tnt;
alter table media.viewing_authorization validate constraint fk_viewing_authorization_student_id_tnt;

-- ── ops
alter table ops.educator_performance_snapshot validate constraint fk_educator_performance_snapshot_educator_id_tnt;
alter table ops.leave_approval validate constraint fk_leave_approval_approver_id_tnt;
alter table ops.leave_approval validate constraint fk_leave_approval_leave_request_id_tnt;
alter table ops.leave_balance validate constraint fk_leave_balance_educator_id_tnt;
alter table ops.leave_request validate constraint fk_leave_request_educator_id_tnt;
alter table ops.meeting validate constraint fk_meeting_campus_id_tnt;
alter table ops.meeting validate constraint fk_meeting_organizer_id_tnt;
alter table ops.meeting validate constraint fk_meeting_room_id_tnt;
alter table ops.meeting_follow_up validate constraint fk_meeting_follow_up_assignee_id_tnt;
alter table ops.meeting_follow_up validate constraint fk_meeting_follow_up_meeting_id_tnt;
alter table ops.meeting_outcome validate constraint fk_meeting_outcome_meeting_id_tnt;
alter table ops.meeting_participant validate constraint fk_meeting_participant_guardian_id_tnt;
alter table ops.meeting_participant validate constraint fk_meeting_participant_lead_id_tnt;
alter table ops.meeting_participant validate constraint fk_meeting_participant_meeting_id_tnt;
alter table ops.meeting_participant validate constraint fk_meeting_participant_user_id_tnt;
alter table ops.parent_feedback validate constraint fk_parent_feedback_educator_id_tnt;
alter table ops.parent_feedback validate constraint fk_parent_feedback_guardian_id_tnt;
alter table ops.parent_feedback validate constraint fk_parent_feedback_session_id_tnt;
alter table ops.survey_response validate constraint fk_survey_response_guardian_id_tnt;
alter table ops.survey_response validate constraint fk_survey_response_respondent_user_id_tnt;
alter table ops.survey_response validate constraint fk_survey_response_session_id_tnt;
alter table ops.survey_response validate constraint fk_survey_response_student_id_tnt;
alter table ops.survey_response validate constraint fk_survey_response_survey_id_tnt;

-- ── scheduling
alter table scheduling.attendance validate constraint fk_attendance_session_id_tnt;
alter table scheduling.attendance validate constraint fk_attendance_student_id_tnt;
alter table scheduling.calendar_entry validate constraint fk_calendar_entry_campus_id_tnt;
alter table scheduling.makeup_request validate constraint fk_makeup_request_makeup_session_id_tnt;
alter table scheduling.makeup_request validate constraint fk_makeup_request_missed_session_id_tnt;
alter table scheduling.makeup_request validate constraint fk_makeup_request_student_id_tnt;
alter table scheduling.recurrence_exception validate constraint fk_recurrence_exception_recurring_schedule_id_tnt;
alter table scheduling.recurring_schedule validate constraint fk_recurring_schedule_campus_id_tnt;
alter table scheduling.recurring_schedule validate constraint fk_recurring_schedule_program_service_id_tnt;
alter table scheduling.recurring_schedule validate constraint fk_recurring_schedule_room_id_tnt;
alter table scheduling.recurring_schedule validate constraint fk_recurring_schedule_student_program_id_tnt;
alter table scheduling.recurring_schedule_educator validate constraint fk_recurring_schedule_educator_educator_id_tnt;
alter table scheduling.recurring_schedule_educator validate constraint fk_recurring_schedule_educator_recurring_schedule_id_tnt;
alter table scheduling.room validate constraint fk_room_campus_id_tnt;
alter table scheduling.session validate constraint fk_session_campus_id_tnt;
alter table scheduling.session validate constraint fk_session_program_service_id_tnt;
alter table scheduling.session validate constraint fk_session_recurring_schedule_id_tnt;
alter table scheduling.session validate constraint fk_session_room_id_tnt;
alter table scheduling.session_educator validate constraint fk_session_educator_educator_id_tnt;
alter table scheduling.session_educator validate constraint fk_session_educator_session_id_tnt;
alter table scheduling.session_goal validate constraint fk_session_goal_session_id_tnt;
alter table scheduling.session_goal validate constraint fk_session_goal_student_goal_id_tnt;
alter table scheduling.session_note validate constraint fk_session_note_authored_by_tnt;
alter table scheduling.session_note validate constraint fk_session_note_session_id_tnt;
alter table scheduling.session_participant validate constraint fk_session_participant_session_id_tnt;
alter table scheduling.session_participant validate constraint fk_session_participant_student_id_tnt;
alter table scheduling.session_participant validate constraint fk_session_participant_student_program_id_tnt;

-- ── students
alter table students.case_note validate constraint fk_case_note_student_id_tnt;
alter table students.development_report validate constraint fk_development_report_file_id_tnt;
alter table students.development_report validate constraint fk_development_report_student_id_tnt;
alter table students.developmental_profile validate constraint fk_developmental_profile_student_id_tnt;
alter table students.diagnosis validate constraint fk_diagnosis_source_file_id_tnt;
alter table students.diagnosis validate constraint fk_diagnosis_student_id_tnt;
alter table students.emergency_contact validate constraint fk_emergency_contact_student_id_tnt;
alter table students.external_institution_report validate constraint fk_external_institution_report_file_id_tnt;
alter table students.external_institution_report validate constraint fk_external_institution_report_student_id_tnt;
alter table students.guardian validate constraint fk_guardian_user_id_tnt;
alter table students.guardian_portal_access validate constraint fk_guardian_portal_access_guardian_id_tnt;
alter table students.guardian_portal_access validate constraint fk_guardian_portal_access_student_id_tnt;
alter table students.medical_report validate constraint fk_medical_report_file_id_tnt;
alter table students.medical_report validate constraint fk_medical_report_student_id_tnt;
alter table students.student validate constraint fk_student_photo_file_id_tnt;
alter table students.student validate constraint fk_student_primary_campus_id_tnt;
alter table students.student_campus validate constraint fk_student_campus_campus_id_tnt;
alter table students.student_campus validate constraint fk_student_campus_student_id_tnt;
alter table students.student_care_assignment validate constraint fk_student_care_assignment_campus_id_tnt;
alter table students.student_care_assignment validate constraint fk_student_care_assignment_educator_id_tnt;
alter table students.student_care_assignment validate constraint fk_student_care_assignment_granted_by_tnt;
alter table students.student_care_assignment validate constraint fk_student_care_assignment_source_assignment_id_tnt;
alter table students.student_care_assignment validate constraint fk_student_care_assignment_student_id_tnt;
alter table students.student_guardian validate constraint fk_student_guardian_guardian_id_tnt;
alter table students.student_guardian validate constraint fk_student_guardian_student_id_tnt;
alter table students.student_status_history validate constraint fk_student_status_history_student_id_tnt;
