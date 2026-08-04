-- =============================================================================
-- AyNesil Platform :: Flyway V32 — Assessment Permissions & Menu
-- =============================================================================
-- Assessment API + Vue routes require assessment_* permission codes, but no
-- prior migration seeded them. V8 top-level 'assessment' menu used student:read,
-- so the item appeared while /assessment still returned frontend 403.
--
-- Objects created / updated:
--   1. Permission catalog (templates, sessions, reports, recommendations)
--   2. Fix assessment root menu required_permission → assessment_session:read
--   3. Assessment sub-menu items + translations
--   4. Grant all new permissions → admin role
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- After apply: users must re-login so JWT picks up new permission claims.
-- =============================================================================


-- ── Step 1: Permission catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  -- Templates
  ('assessment_template:read',       'assessment_template',      'read'),
  ('assessment_template:create',     'assessment_template',      'create'),
  ('assessment_template:update',     'assessment_template',      'update'),
  ('assessment_template:delete',     'assessment_template',      'delete'),
  ('assessment_template:publish',    'assessment_template',      'publish'),
  ('assessment_template:version',    'assessment_template',      'version'),
  -- Sessions
  ('assessment_session:read',        'assessment_session',       'read'),
  ('assessment_session:create',      'assessment_session',       'create'),
  ('assessment_session:update',      'assessment_session',       'update'),
  ('assessment_session:delete',      'assessment_session',       'delete'),
  ('assessment_session:start',       'assessment_session',       'start'),
  ('assessment_session:complete',    'assessment_session',       'complete'),
  ('assessment_session:cancel',      'assessment_session',       'cancel'),
  ('assessment_session:submit_responses', 'assessment_session',  'submit_responses'),
  -- Reports
  ('assessment_report:read',         'assessment_report',        'read'),
  ('assessment_report:create',       'assessment_report',        'create'),
  ('assessment_report:update',       'assessment_report',        'update'),
  ('assessment_report:finalize',     'assessment_report',        'finalize'),
  -- Program recommendations
  ('program_recommendation:read',    'program_recommendation',   'read'),
  ('program_recommendation:create',  'program_recommendation',   'create'),
  ('program_recommendation:update',  'program_recommendation',   'update')
on conflict (code) do nothing;


-- ── Step 2: Fix root assessment menu permission ───────────────────────────────

update iam.menu_item m
set required_permission_id = p.id
from iam.permission p
where m.code = 'assessment'
  and m.corporation_id is null
  and p.code = 'assessment_session:read';


-- ── Step 3: Assessment sub-menu items ─────────────────────────────────────────

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
    ('assessment-dashboard', '/assessment',                    'chart',           10, 'assessment_session:read'),
    ('assessment-sessions',  '/assessment/sessions',           'clipboard-text',  20, 'assessment_session:read'),
    ('assessment-templates', '/assessment/templates',          'document-text',   30, 'assessment_template:read')
) as v (code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'assessment' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('assessment-dashboard', 'tr', 'Özet'),
    ('assessment-dashboard', 'en', 'Dashboard'),
    ('assessment-sessions',  'tr', 'Oturumlar'),
    ('assessment-sessions',  'en', 'Sessions'),
    ('assessment-templates', 'tr', 'Şablonlar'),
    ('assessment-templates', 'en', 'Templates')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;


-- ── Step 4: Grant all new permissions to the admin role ───────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where r.code = 'admin'
  and p.code in (
    'assessment_template:read', 'assessment_template:create',
    'assessment_template:update', 'assessment_template:delete',
    'assessment_template:publish', 'assessment_template:version',
    'assessment_session:read', 'assessment_session:create',
    'assessment_session:update', 'assessment_session:delete',
    'assessment_session:start', 'assessment_session:complete',
    'assessment_session:cancel', 'assessment_session:submit_responses',
    'assessment_report:read', 'assessment_report:create',
    'assessment_report:update', 'assessment_report:finalize',
    'program_recommendation:read', 'program_recommendation:create',
    'program_recommendation:update'
  )
on conflict do nothing;
