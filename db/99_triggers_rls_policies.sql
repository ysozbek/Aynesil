-- =====================================================================
-- AyNesil Platform :: 99 — Cross-cutting wiring (run LAST)
-- 1) updated_at/row_version triggers   2) RLS tenant isolation
-- 3) audit triggers on sensitive data  4) educator scheduling-conflict guard
-- 5) log-table read/write policies
-- NOTE: migrations/seeds run as the table OWNER and bypass RLS (RLS is enabled,
--       not FORCED). The application MUST connect as a separate, non-owner role.
-- =====================================================================

-- Schemas in scope for generic wiring.
-- (kept inline in each DO block below)

-- ---------------------------------------------------------------------
-- 1) Attach updated_at/row_version maintenance to every table with updated_at
-- ---------------------------------------------------------------------
do $$
declare r record;
begin
  for r in
    select n.nspname as sch, c.relname as tbl
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where c.relkind = 'r'
      and n.nspname in ('core','iam','ref','crm','students','assessment','educators',
                        'education','scheduling','finance','legal','media','ops','camps','consultancy')
      and right(c.relname, 8) <> '_default'
      and exists (select 1 from pg_attribute a
                  where a.attrelid = c.oid and a.attname = 'updated_at' and not a.attisdropped)
  loop
    execute format(
      'create or replace trigger trg_set_updated_at before update on %I.%I
         for each row execute function core.set_updated_at()', r.sch, r.tbl);
  end loop;
end $$;

-- ---------------------------------------------------------------------
-- 2) Row-Level Security: tenant isolation on every table with corporation_id
--    (excludes append-only/system tables handled in section 5)
-- ---------------------------------------------------------------------
do $$
declare r record;
begin
  for r in
    select n.nspname as sch, c.relname as tbl
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where c.relkind = 'r'
      and n.nspname in ('core','iam','ref','crm','students','assessment','educators',
                        'education','scheduling','finance','legal','media','ops','camps','consultancy')
      and right(c.relname, 8) <> '_default'
      and not (n.nspname = 'core' and c.relname in ('outbox_event'))
      -- Platform-default menu rows (corporation_id IS NULL) need split policies — see below.
      and not (n.nspname = 'iam' and c.relname = 'menu_item')
      and exists (select 1 from pg_attribute a
                  where a.attrelid = c.oid and a.attname = 'corporation_id' and not a.attisdropped)
  loop
    execute format('alter table %I.%I enable row level security', r.sch, r.tbl);
    execute format('drop policy if exists tenant_isolation on %I.%I', r.sch, r.tbl);
    -- Split per command (V45): global rows (corporation_id IS NULL) are readable and targetable
    -- by UPDATE, but never deletable by a tenant.
    execute format('drop policy if exists tenant_isolation_insert on %I.%I', r.sch, r.tbl);
    execute format('drop policy if exists tenant_isolation_update on %I.%I', r.sch, r.tbl);
    execute format('drop policy if exists tenant_isolation_delete on %I.%I', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation on %I.%I for select
         using (corporation_id is null or corporation_id = core.current_corporation_id())', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation_insert on %I.%I for insert
         with check (corporation_id = core.current_corporation_id())', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation_update on %I.%I for update
         using (corporation_id is null or corporation_id = core.current_corporation_id())
         with check (corporation_id = core.current_corporation_id())', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation_delete on %I.%I for delete
         using (corporation_id = core.current_corporation_id())', r.sch, r.tbl);
  end loop;
end $$;

-- Tables added by later migrations define a strict tenant policy (no `corporation_id IS NULL`
-- branch; the column is NOT NULL there). Mirror them exactly so layer DDL and migrations agree
-- (checked by db/validation/drift.sql): V17 surveys, V22 camp activities, V35 recurring educators.
do $$
declare r record;
begin
  for r in
    select * from (values
      ('ops', 'survey_definition'), ('ops', 'survey_response'),
      ('camps', 'camp_activity'), ('camps', 'camp_educator'), ('camps', 'camp_activity_participation'),
      ('scheduling', 'recurring_schedule_educator')
    ) as t(sch, tbl)
  loop
    execute format('drop policy if exists tenant_isolation on %I.%I', r.sch, r.tbl);
    -- the generic loop above created the split (V45) policies on these tables too
    execute format('drop policy if exists tenant_isolation_insert on %I.%I', r.sch, r.tbl);
    execute format('drop policy if exists tenant_isolation_update on %I.%I', r.sch, r.tbl);
    execute format('drop policy if exists tenant_isolation_delete on %I.%I', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation on %I.%I
         using (corporation_id = core.current_corporation_id())
         with check (corporation_id = core.current_corporation_id())', r.sch, r.tbl);
  end loop;
end $$;

-- iam.menu_item: platform defaults (corporation_id IS NULL) are read-only for tenants (V45);
-- INSERT/UPDATE/DELETE are tenant-scoped.
alter table iam.menu_item enable row level security;
drop policy if exists tenant_isolation on iam.menu_item;
drop policy if exists menu_item_select on iam.menu_item;
drop policy if exists menu_item_insert on iam.menu_item;
drop policy if exists menu_item_update on iam.menu_item;
drop policy if exists menu_item_delete on iam.menu_item;
create policy menu_item_select on iam.menu_item
  for select
  using (corporation_id is null or corporation_id = core.current_corporation_id());
create policy menu_item_insert on iam.menu_item
  for insert
  with check (corporation_id = core.current_corporation_id());
create policy menu_item_update on iam.menu_item
  for update
  using (corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());
create policy menu_item_delete on iam.menu_item
  for delete
  using (corporation_id = core.current_corporation_id());

-- core.corporation (V43, Y1): the tenant root has no corporation_id column; a tenant sees and
-- changes only its own row. Creating/suspending corporations is a platform operation (owner role).
alter table core.corporation enable row level security;
drop policy if exists tenant_isolation on core.corporation;
create policy tenant_isolation on core.corporation
  using (id = core.current_corporation_id())
  with check (id = core.current_corporation_id());

-- Child tables without corporation_id (V44): tenant derived from the parent row.
-- parent_read: parent visible under its own RLS; parent_write: parent owned by the tenant.
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

-- ---------------------------------------------------------------------
-- 3) Generic audit trigger on clinical / financial / legal / scheduling data
-- ---------------------------------------------------------------------
do $$
declare r record;
begin
  for r in
    select n.nspname as sch, c.relname as tbl
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where c.relkind = 'r'
      and n.nspname in ('students','assessment','education','scheduling','finance','legal','media')
      and right(c.relname, 8) <> '_default'
  loop
    execute format(
      'create or replace trigger trg_audit after insert or update or delete on %I.%I
         for each row execute function core.audit_trigger()', r.sch, r.tbl);
  end loop;
end $$;

-- ---------------------------------------------------------------------
-- 4) Educator scheduling-conflict guard (multi-educator sessions can't use a
--    single EXCLUDE constraint, so enforce overlap via trigger)
-- ---------------------------------------------------------------------
create or replace function scheduling.check_educator_overlap()
returns trigger
language plpgsql
as $$
begin
  if exists (
    select 1
    from scheduling.session_educator se
    join scheduling.session s2 on s2.id = se.session_id
    join scheduling.session s1 on s1.id = new.session_id
    where se.educator_id = new.educator_id
      and se.session_id <> new.session_id
      and s1.time_range && s2.time_range
      and s1.status <> 'cancelled' and s1.deleted_at is null
      and s2.status <> 'cancelled' and s2.deleted_at is null
  ) then
    raise exception 'Educator % is already booked in an overlapping session', new.educator_id
      using errcode = 'exclusion_violation';
  end if;
  return new;
end;
$$;

create or replace trigger trg_educator_overlap
  before insert or update on scheduling.session_educator
  for each row execute function scheduling.check_educator_overlap();

-- ---------------------------------------------------------------------
-- 5) Log / append-only tables: tenant-scoped reads, unrestricted writes
--    (written by triggers/system processes; read via reporting layer)
-- ---------------------------------------------------------------------
do $$
declare r record;
begin
  for r in
    select * from (values
      ('core','audit_log'), ('core','activity_log'), ('core','integration_log'),
      ('media','viewing_log')
    ) as t(sch, tbl)
  loop
    execute format('alter table %I.%I enable row level security', r.sch, r.tbl);
    execute format('drop policy if exists log_read on %I.%I',  r.sch, r.tbl);
    execute format('drop policy if exists log_write on %I.%I', r.sch, r.tbl);
    execute format(
      'create policy log_read on %I.%I for select
         using (corporation_id is null or corporation_id = core.current_corporation_id())',
      r.sch, r.tbl);
    execute format(
      'create policy log_write on %I.%I for insert with check (true)',
      r.sch, r.tbl);
  end loop;
end $$;
