-- =============================================================================
-- AyNesil :: V44 — RLS for child tables without corporation_id (Y2, Y3, Y4, Y6 root cause)
-- =============================================================================
-- docs/uyusmazlik-dogrulama.md: the #86/#87 work and the drift.sql invariant only covered
-- tables WITH a corporation_id column. These child tables have none, had RLS disabled and were
-- fully readable/writable by aynesil_app, so a handler that skipped the parent check could
-- read or change another tenant's rows (role_permission, template sections/items,
-- translations, survey answers …). Handlers now check the parent (TenantOwnership); this is
-- the database-side defence: each table gets two policies bound to its parent row.
--
--   parent_read  (SELECT)  the parent row is visible to the tenant under the PARENT's own RLS
--                          (own rows, plus global rows where the parent allows them — e.g.
--                          platform templates, global ref values, platform menu items);
--   parent_write (ALL)     the parent row belongs to the current tenant. Global parents are
--                          read-only for tenants: changing their children would change them for
--                          every tenant.
--
-- Unaffected: owner role (Flyway, seeds, new_tenant.py) bypasses RLS; FK cascades bypass RLS;
-- iam.get_user_permissions() is SECURITY DEFINER (login / refresh).
-- Global catalogs without tenant data (ref.locale, ref.i18n_message, ref.ref_type,
-- iam.permission, core.setting_definition, core.integration_provider) stay without RLS and
-- become read-only for aynesil_app in afterMigrate__app_role_grants.sql; partitions lose their
-- direct grants there too (Y11). db/validation/drift.sql lists every remaining RLS-less table
-- reachable by aynesil_app with its reason and fails on any other.
--
-- Idempotent: policies are dropped/recreated. Mirrored in db/99_triggers_rls_policies.sql.
-- =============================================================================

-- ref.ref_value_translation: tenant via ref.ref_value (ref_value_id)
alter table ref.ref_value_translation enable row level security;
drop policy if exists parent_read on ref.ref_value_translation;
drop policy if exists parent_write on ref.ref_value_translation;
create policy parent_read on ref.ref_value_translation for select
  using (exists (select 1 from ref.ref_value p where p.id = ref_value_translation.ref_value_id));
create policy parent_write on ref.ref_value_translation
  using (exists (select 1 from ref.ref_value p where p.id = ref_value_translation.ref_value_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from ref.ref_value p where p.id = ref_value_translation.ref_value_id and p.corporation_id = core.current_corporation_id()));

-- iam.user_identity: tenant via iam.user_account (user_id)
alter table iam.user_identity enable row level security;
drop policy if exists parent_read on iam.user_identity;
drop policy if exists parent_write on iam.user_identity;
create policy parent_read on iam.user_identity for select
  using (exists (select 1 from iam.user_account p where p.id = user_identity.user_id));
create policy parent_write on iam.user_identity
  using (exists (select 1 from iam.user_account p where p.id = user_identity.user_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from iam.user_account p where p.id = user_identity.user_id and p.corporation_id = core.current_corporation_id()));

-- iam.role_permission: tenant via iam.role (role_id)
alter table iam.role_permission enable row level security;
drop policy if exists parent_read on iam.role_permission;
drop policy if exists parent_write on iam.role_permission;
create policy parent_read on iam.role_permission for select
  using (exists (select 1 from iam.role p where p.id = role_permission.role_id));
create policy parent_write on iam.role_permission
  using (exists (select 1 from iam.role p where p.id = role_permission.role_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from iam.role p where p.id = role_permission.role_id and p.corporation_id = core.current_corporation_id()));

-- iam.menu_item_translation: tenant via iam.menu_item (menu_item_id)
alter table iam.menu_item_translation enable row level security;
drop policy if exists parent_read on iam.menu_item_translation;
drop policy if exists parent_write on iam.menu_item_translation;
create policy parent_read on iam.menu_item_translation for select
  using (exists (select 1 from iam.menu_item p where p.id = menu_item_translation.menu_item_id));
create policy parent_write on iam.menu_item_translation
  using (exists (select 1 from iam.menu_item p where p.id = menu_item_translation.menu_item_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from iam.menu_item p where p.id = menu_item_translation.menu_item_id and p.corporation_id = core.current_corporation_id()));

-- core.notification_template_translation: tenant via core.notification_template (template_id)
alter table core.notification_template_translation enable row level security;
drop policy if exists parent_read on core.notification_template_translation;
drop policy if exists parent_write on core.notification_template_translation;
create policy parent_read on core.notification_template_translation for select
  using (exists (select 1 from core.notification_template p where p.id = notification_template_translation.template_id));
create policy parent_write on core.notification_template_translation
  using (exists (select 1 from core.notification_template p where p.id = notification_template_translation.template_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from core.notification_template p where p.id = notification_template_translation.template_id and p.corporation_id = core.current_corporation_id()));

-- core.notification_delivery: tenant via core.notification (notification_id)
alter table core.notification_delivery enable row level security;
drop policy if exists parent_read on core.notification_delivery;
drop policy if exists parent_write on core.notification_delivery;
create policy parent_read on core.notification_delivery for select
  using (exists (select 1 from core.notification p where p.id = notification_delivery.notification_id));
create policy parent_write on core.notification_delivery
  using (exists (select 1 from core.notification p where p.id = notification_delivery.notification_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from core.notification p where p.id = notification_delivery.notification_id and p.corporation_id = core.current_corporation_id()));

-- core.notification_trigger_channel: tenant via core.notification_trigger_config (trigger_config_id)
alter table core.notification_trigger_channel enable row level security;
drop policy if exists parent_read on core.notification_trigger_channel;
drop policy if exists parent_write on core.notification_trigger_channel;
create policy parent_read on core.notification_trigger_channel for select
  using (exists (select 1 from core.notification_trigger_config p where p.id = notification_trigger_channel.trigger_config_id));
create policy parent_write on core.notification_trigger_channel
  using (exists (select 1 from core.notification_trigger_config p where p.id = notification_trigger_channel.trigger_config_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from core.notification_trigger_config p where p.id = notification_trigger_channel.trigger_config_id and p.corporation_id = core.current_corporation_id()));

-- assessment.assessment_template_translation: tenant via assessment.assessment_template (template_id)
alter table assessment.assessment_template_translation enable row level security;
drop policy if exists parent_read on assessment.assessment_template_translation;
drop policy if exists parent_write on assessment.assessment_template_translation;
create policy parent_read on assessment.assessment_template_translation for select
  using (exists (select 1 from assessment.assessment_template p where p.id = assessment_template_translation.template_id));
create policy parent_write on assessment.assessment_template_translation
  using (exists (select 1 from assessment.assessment_template p where p.id = assessment_template_translation.template_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from assessment.assessment_template p where p.id = assessment_template_translation.template_id and p.corporation_id = core.current_corporation_id()));

-- assessment.assessment_section: tenant via assessment.assessment_template (template_id)
alter table assessment.assessment_section enable row level security;
drop policy if exists parent_read on assessment.assessment_section;
drop policy if exists parent_write on assessment.assessment_section;
create policy parent_read on assessment.assessment_section for select
  using (exists (select 1 from assessment.assessment_template p where p.id = assessment_section.template_id));
create policy parent_write on assessment.assessment_section
  using (exists (select 1 from assessment.assessment_template p where p.id = assessment_section.template_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from assessment.assessment_template p where p.id = assessment_section.template_id and p.corporation_id = core.current_corporation_id()));

-- education.program_translation: tenant via education.program (program_id)
alter table education.program_translation enable row level security;
drop policy if exists parent_read on education.program_translation;
drop policy if exists parent_write on education.program_translation;
create policy parent_read on education.program_translation for select
  using (exists (select 1 from education.program p where p.id = program_translation.program_id));
create policy parent_write on education.program_translation
  using (exists (select 1 from education.program p where p.id = program_translation.program_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from education.program p where p.id = program_translation.program_id and p.corporation_id = core.current_corporation_id()));

-- education.goal_template_translation: tenant via education.goal_template (goal_template_id)
alter table education.goal_template_translation enable row level security;
drop policy if exists parent_read on education.goal_template_translation;
drop policy if exists parent_write on education.goal_template_translation;
create policy parent_read on education.goal_template_translation for select
  using (exists (select 1 from education.goal_template p where p.id = goal_template_translation.goal_template_id));
create policy parent_write on education.goal_template_translation
  using (exists (select 1 from education.goal_template p where p.id = goal_template_translation.goal_template_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from education.goal_template p where p.id = goal_template_translation.goal_template_id and p.corporation_id = core.current_corporation_id()));

-- legal.contract_template_translation: tenant via legal.contract_template (contract_template_id)
alter table legal.contract_template_translation enable row level security;
drop policy if exists parent_read on legal.contract_template_translation;
drop policy if exists parent_write on legal.contract_template_translation;
create policy parent_read on legal.contract_template_translation for select
  using (exists (select 1 from legal.contract_template p where p.id = contract_template_translation.contract_template_id));
create policy parent_write on legal.contract_template_translation
  using (exists (select 1 from legal.contract_template p where p.id = contract_template_translation.contract_template_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from legal.contract_template p where p.id = contract_template_translation.contract_template_id and p.corporation_id = core.current_corporation_id()));

-- legal.consent_template_translation: tenant via legal.consent_template (consent_template_id)
alter table legal.consent_template_translation enable row level security;
drop policy if exists parent_read on legal.consent_template_translation;
drop policy if exists parent_write on legal.consent_template_translation;
create policy parent_read on legal.consent_template_translation for select
  using (exists (select 1 from legal.consent_template p where p.id = consent_template_translation.consent_template_id));
create policy parent_write on legal.consent_template_translation
  using (exists (select 1 from legal.consent_template p where p.id = consent_template_translation.consent_template_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from legal.consent_template p where p.id = consent_template_translation.consent_template_id and p.corporation_id = core.current_corporation_id()));

-- ops.survey_question: tenant via ops.survey_definition (survey_id)
alter table ops.survey_question enable row level security;
drop policy if exists parent_read on ops.survey_question;
drop policy if exists parent_write on ops.survey_question;
create policy parent_read on ops.survey_question for select
  using (exists (select 1 from ops.survey_definition p where p.id = survey_question.survey_id));
create policy parent_write on ops.survey_question
  using (exists (select 1 from ops.survey_definition p where p.id = survey_question.survey_id and p.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from ops.survey_definition p where p.id = survey_question.survey_id and p.corporation_id = core.current_corporation_id()));

-- assessment.assessment_item: tenant via section → assessment_template
alter table assessment.assessment_item enable row level security;
drop policy if exists parent_read on assessment.assessment_item;
drop policy if exists parent_write on assessment.assessment_item;
create policy parent_read on assessment.assessment_item for select
  using (exists (select 1 from assessment.assessment_section s join assessment.assessment_template t on t.id = s.template_id where s.id = assessment_item.section_id));
create policy parent_write on assessment.assessment_item
  using (exists (select 1 from assessment.assessment_section s join assessment.assessment_template t on t.id = s.template_id where s.id = assessment_item.section_id and t.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from assessment.assessment_section s join assessment.assessment_template t on t.id = s.template_id where s.id = assessment_item.section_id and t.corporation_id = core.current_corporation_id()));

-- ops.survey_answer_option: tenant via question → survey_definition
alter table ops.survey_answer_option enable row level security;
drop policy if exists parent_read on ops.survey_answer_option;
drop policy if exists parent_write on ops.survey_answer_option;
create policy parent_read on ops.survey_answer_option for select
  using (exists (select 1 from ops.survey_question q join ops.survey_definition d on d.id = q.survey_id where q.id = survey_answer_option.question_id));
create policy parent_write on ops.survey_answer_option
  using (exists (select 1 from ops.survey_question q join ops.survey_definition d on d.id = q.survey_id where q.id = survey_answer_option.question_id and d.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from ops.survey_question q join ops.survey_definition d on d.id = q.survey_id where q.id = survey_answer_option.question_id and d.corporation_id = core.current_corporation_id()));

-- ops.survey_question_response: tenant via survey_response; the question must belong to the same tenant (Y6)
alter table ops.survey_question_response enable row level security;
drop policy if exists parent_read on ops.survey_question_response;
drop policy if exists parent_write on ops.survey_question_response;
create policy parent_read on ops.survey_question_response for select
  using (exists (select 1 from ops.survey_response r where r.id = survey_question_response.response_id));
create policy parent_write on ops.survey_question_response
  using (exists (select 1 from ops.survey_response r where r.id = survey_question_response.response_id and r.corporation_id = core.current_corporation_id()) and exists (select 1 from ops.survey_question q join ops.survey_definition d on d.id = q.survey_id where q.id = survey_question_response.question_id and d.corporation_id = core.current_corporation_id()))
  with check (exists (select 1 from ops.survey_response r where r.id = survey_question_response.response_id and r.corporation_id = core.current_corporation_id()) and exists (select 1 from ops.survey_question q join ops.survey_definition d on d.id = q.survey_id where q.id = survey_question_response.question_id and d.corporation_id = core.current_corporation_id()));
