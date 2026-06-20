-- =============================================================================
-- AyNesil Platform :: Flyway V10 — Students Module Permissions + Menu Items
-- =============================================================================
-- Adds granular permission codes for the Students, Guardians and Case
-- Management module, then seeds sub-navigation items under the existing
-- 'students' top-level menu item (seeded in V8).
--
-- Note: 'student:read' and 'student:write' were seeded in V6 — this
-- migration extends the catalog with action-level granularity.
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================

-- ── Step 1: Students Module Permission Catalog ────────────────────────────────

insert into iam.permission (code, resource, action) values
  -- Students
  ('student:create',              'student',    'create'),
  ('student:update',              'student',    'update'),
  ('student:delete',              'student',    'delete'),
  ('student:change_status',       'student',    'change_status'),
  -- Guardians
  ('guardian:read',               'guardian',   'read'),
  ('guardian:create',             'guardian',   'create'),
  ('guardian:update',             'guardian',   'update'),
  ('guardian:delete',             'guardian',   'delete'),
  ('guardian:manage_portal',      'guardian',   'manage_portal'),
  -- Case Notes (clinical-access gated)
  ('case_note:read',              'case_note',  'read'),
  ('case_note:create',            'case_note',  'create'),
  ('case_note:update',            'case_note',  'update'),
  ('case_note:delete',            'case_note',  'delete'),
  ('case_note:read_confidential', 'case_note',  'read_confidential')
on conflict (code) do nothing;

-- ── Step 2: Students Sub-menu Items ──────────────────────────────────────────

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
    -- code                          route                          icon               sort  perm_code
    ('students-list',                '/students',                   'profile-2user',    10,  'student:read'),
    ('students-guardians',           '/students/guardians',         'people',           20,  'guardian:read'),
    ('students-case-management',     '/students/case-management',   'clipboard-text',   30,  'case_note:read'),
    ('students-portal-access',       '/students/portal-access',     'shield-tick',      40,  'guardian:manage_portal'),
    ('students-new',                 '/students/new',               'user-add',         50,  'student:create')
) as v(code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'students' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 3: Turkish and English translations ──────────────────────────────────

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('students-list',              'tr', 'Öğrenci Listesi'),
    ('students-list',              'en', 'Students'),
    ('students-guardians',         'tr', 'Veliler'),
    ('students-guardians',         'en', 'Guardians'),
    ('students-case-management',   'tr', 'Vaka Yönetimi'),
    ('students-case-management',   'en', 'Case Management'),
    ('students-portal-access',     'tr', 'Veli Portal Erişimi'),
    ('students-portal-access',     'en', 'Parent Portal Access'),
    ('students-new',               'tr', 'Yeni Öğrenci'),
    ('students-new',               'en', 'New Student')
) as t(code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
