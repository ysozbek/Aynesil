-- =============================================================================
-- AyNesil Platform :: Flyway V17 — Notification & Parent Portal DDL Migration
-- =============================================================================
-- Implements the Notification (trigger config) and Parent Portal (survey/feedback)
-- module additions. Purely ADDITIVE — no existing table, column, or FK is modified.
--
-- Objects created / seeded:
--   1. Table:       core.notification_trigger_config (+ RLS + triggers)
--   2. Table:       core.notification_trigger_channel (junction: trigger_config → ref_value channel)
--   3. Tables:      ops.survey_definition, ops.survey_question,
--                   ops.survey_answer_option, ops.survey_response,
--                   ops.survey_question_response (+ RLS + triggers where applicable)
--   4. Ref type:    survey_type
--   5. Ref values:  notification_type (9 defaults), notification_category (7 defaults),
--                   survey_type (4 defaults) — 20 new ref_values total
--   6. Translations: tr + en for all 20 new ref_values (40 rows)
--   7. Permissions: notification_template:*, notification_trigger:manage,
--                   survey:*, parent_feedback:*, portal:access  (13 new)
--   8. Menu items:  notifications, surveys sub-items + translations
--   9. Role grants: all new permissions → admin role
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================


-- ── Step 1: Configurable Notification Trigger Rules ───────────────────────────

create table core.notification_trigger_config (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid references core.corporation(id),             -- NULL = platform default rule
  trigger_code   text not null,
  -- 'session_reminder' | 'session_completed' | 'session_cancelled' |
  -- 'educator_reassigned' | 'package_expiring' | 'package_expired' |
  -- 'assessment_reminder' | 'meeting_reminder' | 'goal_milestone'
  template_id    uuid references core.notification_template(id),
  -- When to fire relative to the triggering event.
  -- Negative = before event (e.g. -1440 = 24 h before session).
  -- Zero or positive = at / after event.
  offset_minutes integer not null default 0,
  is_active      boolean not null default true,
  created_at     timestamptz not null default now(),
  updated_at     timestamptz not null default now(),
  created_by     uuid,
  row_version    integer not null default 1,
  unique nulls not distinct (corporation_id, trigger_code)
);
comment on table core.notification_trigger_config is
  'Business-configurable mapping: trigger event code → notification template + timing offset. '
  'Channels are managed via core.notification_trigger_channel (junction). '
  'One row per (tenant, trigger_code). NULL corporation_id = platform default, overridable per tenant.';
comment on column core.notification_trigger_config.trigger_code is
  'Stable application event code, e.g. ''session_reminder''. '
  'Matches the code constant used in domain-event handlers.';
comment on column core.notification_trigger_config.offset_minutes is
  'Signed offset from the event timestamp. '
  '-1440 = 24 h before, 0 = at event time, 60 = 1 h after.';

create index ix_notification_trigger_config_corp
  on core.notification_trigger_config (corporation_id, trigger_code)
  where is_active;

alter table core.notification_trigger_config enable row level security;

drop policy if exists tenant_isolation on core.notification_trigger_config;
create policy tenant_isolation on core.notification_trigger_config
  using  (corporation_id is null or corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());

create or replace trigger trg_set_updated_at
  before update on core.notification_trigger_config
  for each row execute function core.set_updated_at();

create or replace trigger trg_audit
  after insert or update or delete on core.notification_trigger_config
  for each row execute function core.audit_trigger();


-- ── Step 2: Notification trigger channel junction ─────────────────────────────
-- Maps a trigger config to one or more notification channels (ref_value 'notification_channel').
-- Replaces the previous text[] approach; adding a new channel requires only an INSERT.

create table core.notification_trigger_channel (
  id               uuid primary key default core.uuid_generate_v7(),
  trigger_config_id uuid not null
    references core.notification_trigger_config(id) on delete cascade,
  channel_id        uuid not null
    references ref.ref_value(id),                                  -- ref_type 'notification_channel'
  unique (trigger_config_id, channel_id)
);
create index ix_notification_trigger_channel_config
  on core.notification_trigger_channel (trigger_config_id);
comment on table core.notification_trigger_channel is
  'Junction: one trigger config → many channels (email, sms, push, in_app, future). '
  'channel_id → ref.ref_value(notification_channel).';


-- ── Step 3: Survey definition ─────────────────────────────────────────────────

create table ops.survey_definition (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  type_id         uuid references ref.ref_value(id),               -- ref_type 'survey_type'
  title           text not null,
  description     text,
  target          text not null default 'guardian'
                    check (target in ('guardian', 'educator', 'student')),
  is_active       boolean not null default true,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table ops.survey_definition is
  'Configurable survey / feedback form header. '
  'target: guardian = parent portal, educator = internal, student = direct.';

alter table ops.survey_definition enable row level security;

drop policy if exists tenant_isolation on ops.survey_definition;
create policy tenant_isolation on ops.survey_definition
  using  (corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());

create or replace trigger trg_set_updated_at
  before update on ops.survey_definition
  for each row execute function core.set_updated_at();

create or replace trigger trg_audit
  after insert or update or delete on ops.survey_definition
  for each row execute function core.audit_trigger();


-- ── Step 4: Survey questions ──────────────────────────────────────────────────
-- No corporation_id: child of survey_definition; tenant isolation flows from parent RLS.
-- Emsal: assessment.assessment_section (child tablolar corporation_id taşımaz).

create table ops.survey_question (
  id              uuid primary key default core.uuid_generate_v7(),
  survey_id       uuid not null references ops.survey_definition(id) on delete cascade,
  question_text   text not null,
  question_type   text not null default 'text'
                    check (question_type in
                           ('text', 'rating', 'yes_no', 'multiple_choice', 'scale')),
  is_required     boolean not null default false,
  sort_order      integer not null default 0,
  created_at      timestamptz not null default now(),
  created_by      uuid
);
create index ix_survey_question_survey on ops.survey_question(survey_id, sort_order);
comment on column ops.survey_question.question_type is
  'text=free text, rating=1–5 stars, yes_no=boolean, '
  'multiple_choice=pick one/many from options, scale=numeric range.';


-- ── Step 5: Answer options (for multiple_choice / scale) ──────────────────────

create table ops.survey_answer_option (
  id           uuid primary key default core.uuid_generate_v7(),
  question_id  uuid not null references ops.survey_question(id) on delete cascade,
  option_text  text not null,
  option_value text,
  sort_order   integer not null default 0
);
create index ix_survey_answer_option_question
  on ops.survey_answer_option(question_id, sort_order);


-- ── Step 6: Survey responses ──────────────────────────────────────────────────

create table ops.survey_response (
  id                  uuid primary key default core.uuid_generate_v7(),
  corporation_id      uuid not null references core.corporation(id),
  survey_id           uuid not null references ops.survey_definition(id),
  respondent_user_id  uuid references iam.user_account(id),
  guardian_id         uuid references students.guardian(id),
  student_id          uuid references students.student(id),
  session_id          uuid references scheduling.session(id),
  submitted_at        timestamptz,
  created_at          timestamptz not null default now()
);
create index ix_survey_response_survey
  on ops.survey_response(survey_id, submitted_at desc);
create index ix_survey_response_guardian
  on ops.survey_response(guardian_id, survey_id)
  where guardian_id is not null;
create index ix_survey_response_student
  on ops.survey_response(student_id, survey_id)
  where student_id is not null;
comment on table ops.survey_response is
  'One submitted response per survey × respondent × optional session. '
  'submitted_at null while the respondent is mid-form.';

alter table ops.survey_response enable row level security;

drop policy if exists tenant_isolation on ops.survey_response;
create policy tenant_isolation on ops.survey_response
  using  (corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());


-- ── Step 7: Per-question answers ─────────────────────────────────────────────

create table ops.survey_question_response (
  id               uuid primary key default core.uuid_generate_v7(),
  response_id      uuid not null references ops.survey_response(id) on delete cascade,
  question_id      uuid not null references ops.survey_question(id),
  answer_text      text,
  answer_option_id uuid references ops.survey_answer_option(id),
  numeric_value    numeric(6, 2),
  unique (response_id, question_id)
);
comment on table ops.survey_question_response is
  'Exactly one of answer_text / answer_option_id / numeric_value per row, '
  'depending on question_type.';


-- ── Step 8: Reference type — survey_type ─────────────────────────────────────

insert into ref.ref_type (code, name, is_system, is_hierarchical, allows_tenant_values) values
  ('survey_type', 'Survey Types', false, false, true)
on conflict (code) do nothing;


-- ── Step 9: Default reference values ─────────────────────────────────────────

insert into ref.ref_value (ref_type_id, code, sort_order, is_default, is_system)
select ref.type_id(v.type_code), v.code, v.sort_order, v.is_default, v.is_system
from (values
  -- notification_type (9 platform defaults; tenants may extend)
  ('notification_type', 'session_reminder',        1, true,  false),
  ('notification_type', 'session_completed',        2, false, false),
  ('notification_type', 'session_cancelled',        3, false, false),
  ('notification_type', 'educator_reassigned',      4, false, false),
  ('notification_type', 'package_expiring',         5, false, false),
  ('notification_type', 'package_expired',          6, false, false),
  ('notification_type', 'assessment_reminder',      7, false, false),
  ('notification_type', 'meeting_reminder',         8, false, false),
  ('notification_type', 'goal_milestone',           9, false, false),
  -- notification_category (7 platform defaults)
  ('notification_category', 'sessions',     1, true,  false),
  ('notification_category', 'finance',      2, false, false),
  ('notification_category', 'assessments',  3, false, false),
  ('notification_category', 'meetings',     4, false, false),
  ('notification_category', 'documents',    5, false, false),
  ('notification_category', 'system',       6, false, false),
  ('notification_category', 'portal',       7, false, false),
  -- survey_type (4 defaults; tenants may extend)
  ('survey_type', 'satisfaction_survey',       1, true,  false),
  ('survey_type', 'session_feedback',          2, false, false),
  ('survey_type', 'post_assessment_feedback',  3, false, false),
  ('survey_type', 'general_feedback',          4, false, false)
) as v (type_code, code, sort_order, is_default, is_system)
on conflict do nothing;


-- ── Step 10: Translations (tr + en) ───────────────────────────────────────────

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  -- notification_type
  ('notification_type', 'session_reminder',     'tr', 'Seans Hatırlatması'),
  ('notification_type', 'session_reminder',     'en', 'Session Reminder'),
  ('notification_type', 'session_completed',    'tr', 'Seans Tamamlandı'),
  ('notification_type', 'session_completed',    'en', 'Session Completed'),
  ('notification_type', 'session_cancelled',    'tr', 'Seans İptal Edildi'),
  ('notification_type', 'session_cancelled',    'en', 'Session Cancelled'),
  ('notification_type', 'educator_reassigned',  'tr', 'Eğitimci Değiştirildi'),
  ('notification_type', 'educator_reassigned',  'en', 'Educator Reassigned'),
  ('notification_type', 'package_expiring',     'tr', 'Paket Sona Eriyor'),
  ('notification_type', 'package_expiring',     'en', 'Package Expiring'),
  ('notification_type', 'package_expired',      'tr', 'Paket Süresi Doldu'),
  ('notification_type', 'package_expired',      'en', 'Package Expired'),
  ('notification_type', 'assessment_reminder',  'tr', 'Değerlendirme Hatırlatması'),
  ('notification_type', 'assessment_reminder',  'en', 'Assessment Reminder'),
  ('notification_type', 'meeting_reminder',     'tr', 'Toplantı Hatırlatması'),
  ('notification_type', 'meeting_reminder',     'en', 'Meeting Reminder'),
  ('notification_type', 'goal_milestone',       'tr', 'Hedef Dönüm Noktası'),
  ('notification_type', 'goal_milestone',       'en', 'Goal Milestone'),
  -- notification_category
  ('notification_category', 'sessions',     'tr', 'Seanslar'),
  ('notification_category', 'sessions',     'en', 'Sessions'),
  ('notification_category', 'finance',      'tr', 'Finans'),
  ('notification_category', 'finance',      'en', 'Finance'),
  ('notification_category', 'assessments',  'tr', 'Değerlendirmeler'),
  ('notification_category', 'assessments',  'en', 'Assessments'),
  ('notification_category', 'meetings',     'tr', 'Toplantılar'),
  ('notification_category', 'meetings',     'en', 'Meetings'),
  ('notification_category', 'documents',    'tr', 'Belgeler'),
  ('notification_category', 'documents',    'en', 'Documents'),
  ('notification_category', 'system',       'tr', 'Sistem'),
  ('notification_category', 'system',       'en', 'System'),
  ('notification_category', 'portal',       'tr', 'Veli Portalı'),
  ('notification_category', 'portal',       'en', 'Parent Portal'),
  -- survey_type
  ('survey_type', 'satisfaction_survey',      'tr', 'Memnuniyet Anketi'),
  ('survey_type', 'satisfaction_survey',      'en', 'Satisfaction Survey'),
  ('survey_type', 'session_feedback',         'tr', 'Seans Geri Bildirimi'),
  ('survey_type', 'session_feedback',         'en', 'Session Feedback'),
  ('survey_type', 'post_assessment_feedback', 'tr', 'Değerlendirme Sonrası Geri Bildirim'),
  ('survey_type', 'post_assessment_feedback', 'en', 'Post-Assessment Feedback'),
  ('survey_type', 'general_feedback',         'tr', 'Genel Geri Bildirim'),
  ('survey_type', 'general_feedback',         'en', 'General Feedback')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code        = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 11: Permission catalog ───────────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  -- Notification template management (admin / content staff)
  ('notification_template:read',   'notification_template', 'read'),
  ('notification_template:create', 'notification_template', 'create'),
  ('notification_template:update', 'notification_template', 'update'),
  ('notification_template:delete', 'notification_template', 'delete'),
  -- Notification trigger configuration (admin)
  ('notification_trigger:manage',  'notification_trigger',  'manage'),
  -- Surveys (admin / content staff)
  ('survey:read',    'survey', 'read'),
  ('survey:create',  'survey', 'create'),
  ('survey:update',  'survey', 'update'),
  ('survey:delete',  'survey', 'delete'),
  -- Survey response (guardians / educators submitting a form)
  ('survey:respond', 'survey', 'respond'),
  -- Parent feedback (simple star-rating)
  ('parent_feedback:read',   'parent_feedback', 'read'),
  ('parent_feedback:create', 'parent_feedback', 'create'),
  -- Parent portal access (assigned to guardian user accounts)
  ('portal:access', 'portal', 'access')
on conflict (code) do nothing;


-- ── Step 12: Menu items ───────────────────────────────────────────────────────

-- Notifications top-level menu item
insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select
    null,
    null,
    v.code,
    v.route,
    v.icon,
    v.sort_order,
    p.id,
    true
from (values
    ('notifications', '/notifications', 'bell', 70, 'notification:read')
) as v (code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('notifications', 'tr', 'Bildirimler'),
    ('notifications', 'en', 'Notifications')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- Notification sub-items under 'notifications'
insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select
    null,
    parent.id,
    v.code,
    v.route,
    v.icon,
    v.sort_order,
    p.id,
    true
from (values
    ('notifications-inbox',     '/notifications/inbox',     'inbox',    10, 'notification:read'),
    ('notifications-templates', '/notifications/templates', 'template', 20, 'notification_template:read'),
    ('notifications-triggers',  '/notifications/triggers',  'zap',      30, 'notification_trigger:manage')
) as v (code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'notifications' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('notifications-inbox',     'tr', 'Gelen Kutusu'),
    ('notifications-inbox',     'en', 'Inbox'),
    ('notifications-templates', 'tr', 'Şablonlar'),
    ('notifications-templates', 'en', 'Templates'),
    ('notifications-triggers',  'tr', 'Tetikleyiciler'),
    ('notifications-triggers',  'en', 'Triggers')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- Surveys sub-menu under 'students' (parent feedback lives in the student context)
insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select
    null,
    parent.id,
    v.code,
    v.route,
    v.icon,
    v.sort_order,
    p.id,
    true
from (values
    ('students-surveys',  '/students/surveys',  'clipboard-list', 70, 'survey:read'),
    ('students-feedback', '/students/feedback', 'star',           80, 'parent_feedback:read')
) as v (code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'students' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('students-surveys',  'tr', 'Anketler'),
    ('students-surveys',  'en', 'Surveys'),
    ('students-feedback', 'tr', 'Veli Geri Bildirimi'),
    ('students-feedback', 'en', 'Parent Feedback')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;


-- ── Step 13: Grant all new permissions to the admin role ──────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where p.code in (
  'notification_template:read', 'notification_template:create',
  'notification_template:update', 'notification_template:delete',
  'notification_trigger:manage',
  'survey:read', 'survey:create', 'survey:update', 'survey:delete', 'survey:respond',
  'parent_feedback:read', 'parent_feedback:create',
  'portal:access'
)
  and r.name = 'admin'
on conflict do nothing;
