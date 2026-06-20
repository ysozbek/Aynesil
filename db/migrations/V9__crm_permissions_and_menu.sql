-- =============================================================================
-- AyNesil Platform :: Flyway V9 — CRM Module Permissions + Menu Items
-- =============================================================================
-- Adds permission codes for the CRM / Lead Management module and seeds the
-- CRM sub-navigation items under the existing 'crm' top-level menu item.
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================

-- ── Step 1: CRM Permission Catalog ───────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  -- Leads
  ('lead:read',            'lead',            'read'),
  ('lead:create',          'lead',            'create'),
  ('lead:update',          'lead',            'update'),
  ('lead:delete',          'lead',            'delete'),
  ('lead:convert',         'lead',            'convert'),
  ('lead:assign',          'lead',            'assign'),
  -- Lead Activities
  ('lead_activity:read',   'lead_activity',   'read'),
  ('lead_activity:create', 'lead_activity',   'create'),
  -- Interviews
  ('interview:read',       'interview',       'read'),
  ('interview:create',     'interview',       'create'),
  ('interview:update',     'interview',       'update'),
  ('interview:manage',     'interview',       'manage')
on conflict (code) do nothing;

-- ── Step 2: CRM Sub-menu Items (children of 'crm' root item) ─────────────────

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
    -- code                   route                          icon                  sort  perm_code
    ('crm-leads',             '/crm/leads',                  'profile-2user',        10,  'lead:read'),
    ('crm-pipeline',          '/crm/pipeline',               'kanban',               20,  'lead:read'),
    ('crm-activities',        '/crm/activities',             'task-square',          30,  'lead_activity:read'),
    ('crm-interviews',        '/crm/interviews',             'message-question',     40,  'interview:read'),
    ('crm-reports',           '/crm/reports',                'chart-pie',            50,  'lead:read')
) as v(code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'crm' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 3: Turkish and English translations for CRM sub-items ───────────────

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('crm-leads',       'tr', 'Adaylar'),
    ('crm-leads',       'en', 'Leads'),
    ('crm-pipeline',    'tr', 'Pipeline'),
    ('crm-pipeline',    'en', 'Pipeline'),
    ('crm-activities',  'tr', 'Aktiviteler'),
    ('crm-activities',  'en', 'Activities'),
    ('crm-interviews',  'tr', 'Görüşmeler'),
    ('crm-interviews',  'en', 'Interviews'),
    ('crm-reports',     'tr', 'CRM Raporları'),
    ('crm-reports',     'en', 'CRM Reports')
) as t(code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
