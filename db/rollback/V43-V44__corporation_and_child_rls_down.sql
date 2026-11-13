-- =============================================================================
-- Manual rollback for V43 (core.corporation RLS) and V44 (child-table RLS).
-- NOT run by Flyway. Run as the owner role, then delete the V43/V44 rows from
-- flyway_schema_history. The platform-only permissions removed by V43 are NOT re-granted
-- (that would reopen Y1); grant them explicitly if really needed.
-- afterMigrate__app_role_grants.sql keeps revoking partition / catalog writes (Y11).
-- =============================================================================
begin;

drop policy if exists parent_read on ref.ref_value_translation;
drop policy if exists parent_write on ref.ref_value_translation;
alter table ref.ref_value_translation disable row level security;

drop policy if exists parent_read on iam.user_identity;
drop policy if exists parent_write on iam.user_identity;
alter table iam.user_identity disable row level security;

drop policy if exists parent_read on iam.role_permission;
drop policy if exists parent_write on iam.role_permission;
alter table iam.role_permission disable row level security;

drop policy if exists parent_read on iam.menu_item_translation;
drop policy if exists parent_write on iam.menu_item_translation;
alter table iam.menu_item_translation disable row level security;

drop policy if exists parent_read on core.notification_template_translation;
drop policy if exists parent_write on core.notification_template_translation;
alter table core.notification_template_translation disable row level security;

drop policy if exists parent_read on core.notification_delivery;
drop policy if exists parent_write on core.notification_delivery;
alter table core.notification_delivery disable row level security;

drop policy if exists parent_read on core.notification_trigger_channel;
drop policy if exists parent_write on core.notification_trigger_channel;
alter table core.notification_trigger_channel disable row level security;

drop policy if exists parent_read on assessment.assessment_template_translation;
drop policy if exists parent_write on assessment.assessment_template_translation;
alter table assessment.assessment_template_translation disable row level security;

drop policy if exists parent_read on assessment.assessment_section;
drop policy if exists parent_write on assessment.assessment_section;
alter table assessment.assessment_section disable row level security;

drop policy if exists parent_read on education.program_translation;
drop policy if exists parent_write on education.program_translation;
alter table education.program_translation disable row level security;

drop policy if exists parent_read on education.goal_template_translation;
drop policy if exists parent_write on education.goal_template_translation;
alter table education.goal_template_translation disable row level security;

drop policy if exists parent_read on legal.contract_template_translation;
drop policy if exists parent_write on legal.contract_template_translation;
alter table legal.contract_template_translation disable row level security;

drop policy if exists parent_read on legal.consent_template_translation;
drop policy if exists parent_write on legal.consent_template_translation;
alter table legal.consent_template_translation disable row level security;

drop policy if exists parent_read on ops.survey_question;
drop policy if exists parent_write on ops.survey_question;
alter table ops.survey_question disable row level security;

drop policy if exists parent_read on assessment.assessment_item;
drop policy if exists parent_write on assessment.assessment_item;
alter table assessment.assessment_item disable row level security;

drop policy if exists parent_read on ops.survey_answer_option;
drop policy if exists parent_write on ops.survey_answer_option;
alter table ops.survey_answer_option disable row level security;

drop policy if exists parent_read on ops.survey_question_response;
drop policy if exists parent_write on ops.survey_question_response;
alter table ops.survey_question_response disable row level security;

drop policy if exists tenant_isolation on core.corporation;
alter table core.corporation disable row level security;

commit;
