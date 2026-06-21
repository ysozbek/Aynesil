-- =============================================================================
-- AyNesil Platform :: Flyway V11 — Educator & Program Management Permissions + Menu
-- =============================================================================
-- Adds permission codes for the Educators and Programs (Education) modules,
-- then seeds top-level + sub-navigation items.
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================

-- ── Step 1: Permission Catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  -- Educators
  ('educator:read',                 'educator',   'read'),
  ('educator:create',               'educator',   'create'),
  ('educator:update',               'educator',   'update'),
  ('educator:delete',               'educator',   'delete'),
  ('educator:manage_specialties',   'educator',   'manage_specialties'),
  ('educator:manage_campuses',      'educator',   'manage_campuses'),
  ('educator:manage_certifications','educator',   'manage_certifications'),
  ('educator:manage_hierarchy',     'educator',   'manage_hierarchy'),
  -- Programs
  ('program:read',                  'program',    'read'),
  ('program:create',                'program',    'create'),
  ('program:update',                'program',    'update'),
  ('program:delete',                'program',    'delete'),
  -- Enrollments
  ('enrollment:read',               'enrollment', 'read'),
  ('enrollment:create',             'enrollment', 'create'),
  ('enrollment:update',             'enrollment', 'update'),
  ('enrollment:manage_programs',    'enrollment', 'manage_programs')
on conflict (code) do nothing;

-- ── Step 2: Top-level Menu Items ──────────────────────────────────────────────

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
    ('educators', '/educators', 'teacher',  30, 'educator:read'),
    ('programs',  '/programs',  'book',     40, 'program:read')
) as v(code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 3: Educators Sub-menu Items ─────────────────────────────────────────

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
    ('educators-list',        '/educators',               'teacher',          10, 'educator:read'),
    ('educators-hierarchy',   '/educators/hierarchy',     'hierarchy',        20, 'educator:read'),
    ('educators-utilization', '/educators/utilization',   'chart',            30, 'educator:read'),
    ('educators-new',         '/educators/new',           'user-add',         40, 'educator:create')
) as v(code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'educators' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 4: Programs Sub-menu Items ──────────────────────────────────────────

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
    ('programs-list',        '/programs',             'book',           10, 'program:read'),
    ('programs-enrollments', '/programs/enrollments', 'document-text',  20, 'enrollment:read'),
    ('programs-new',         '/programs/new',         'add-circle',     30, 'program:create')
) as v(code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'programs' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 5: Translations (tr + en) ───────────────────────────────────────────

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('educators',             'tr', 'Eğitmenler'),
    ('educators',             'en', 'Educators'),
    ('educators-list',        'tr', 'Eğitmen Listesi'),
    ('educators-list',        'en', 'Educator List'),
    ('educators-hierarchy',   'tr', 'Hiyerarşi'),
    ('educators-hierarchy',   'en', 'Hierarchy'),
    ('educators-utilization', 'tr', 'Doluluk Raporu'),
    ('educators-utilization', 'en', 'Utilization'),
    ('educators-new',         'tr', 'Yeni Eğitmen'),
    ('educators-new',         'en', 'New Educator'),
    ('programs',              'tr', 'Programlar'),
    ('programs',              'en', 'Programs'),
    ('programs-list',         'tr', 'Program Listesi'),
    ('programs-list',         'en', 'Program List'),
    ('programs-enrollments',  'tr', 'Kayıtlar'),
    ('programs-enrollments',  'en', 'Enrollments'),
    ('programs-new',          'tr', 'Yeni Program'),
    ('programs-new',          'en', 'New Program')
) as t(code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- ── Step 6: Grant permissions to default admin role ───────────────────────────
-- Assign all new permissions to the 'admin' role for the Akran tenant.
-- Adjust corporation_id / role name to match target tenant bootstrap.

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where p.code in (
    'educator:read','educator:create','educator:update','educator:delete',
    'educator:manage_specialties','educator:manage_campuses',
    'educator:manage_certifications','educator:manage_hierarchy',
    'program:read','program:create','program:update','program:delete',
    'enrollment:read','enrollment:create','enrollment:update','enrollment:manage_programs'
)
  and r.name = 'admin'
on conflict do nothing;
