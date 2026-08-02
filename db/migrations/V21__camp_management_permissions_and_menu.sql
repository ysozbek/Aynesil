-- =============================================================================
-- AyNesil Platform :: Flyway V21 — Camp Management Permissions & Menu
-- =============================================================================
-- Purely ADDITIVE. Objects created / seeded:
--   1. Permissions: camp:*, camp_enrollment:*, camp_attendance:*, camp_report:*
--   2. Menu items: Camps top-level + sub-items (definitions, enrollments,
--      attendance, reports) with tr + en translations
--   3. Role grants: admin (only seeded role; see 02_akran_bootstrap.sql)
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================


-- ── Step 1: Permission catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action, description) values
  ('camp:read',            'camp', 'read',            'View camp definitions and periods'),
  ('camp:create',          'camp', 'create',          'Create a new camp'),
  ('camp:update',          'camp', 'update',          'Update camp details'),
  ('camp:delete',          'camp', 'delete',          'Delete (soft) a camp'),
  ('camp:activate',        'camp', 'activate',        'Activate or deactivate a camp'),
  ('camp:manage_periods',  'camp', 'manage_periods',  'Create, update, or delete camp periods'),
  ('camp_enrollment:read',     'camp_enrollment', 'read',     'View camp enrollments'),
  ('camp_enrollment:enroll',   'camp_enrollment', 'enroll',   'Enroll a student in a camp period'),
  ('camp_enrollment:manage',   'camp_enrollment', 'manage',   'Move enrollments between enrolled and waitlist'),
  ('camp_enrollment:withdraw', 'camp_enrollment', 'withdraw', 'Withdraw a student from a camp period'),
  ('camp_enrollment:complete', 'camp_enrollment', 'complete', 'Mark a camp enrollment as completed'),
  ('camp_attendance:read',   'camp_attendance', 'read',   'View camp attendance records'),
  ('camp_attendance:record', 'camp_attendance', 'record', 'Record and correct camp attendance'),
  ('camp_report:read',   'camp_report', 'read',   'View camp student reports'),
  ('camp_report:create', 'camp_report', 'create', 'Create a camp student report')
on conflict (code) do nothing;


-- ── Step 2: Role grants ───────────────────────────────────────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.role r
cross join iam.permission p
where r.code = 'admin'
  and p.code like 'camp%'
on conflict do nothing;


-- ── Step 3: Menu items (platform default — corporation_id NULL) ────────────────

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
    ('camps', '/camps', 'tent', 55, 'camp:read')
) as v (code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('camps', 'tr', 'Kamp Yönetimi'),
    ('camps', 'en', 'Camp Management')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- Sub-items under camps
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
    ('camps.definitions', '/camps/definitions', 'list',         1, 'camp:read'),
    ('camps.enrollments', '/camps/enrollments', 'users',        2, 'camp_enrollment:read'),
    ('camps.attendance',  '/camps/attendance',  'check-square', 3, 'camp_attendance:read'),
    ('camps.reports',     '/camps/reports',     'bar-chart',    4, 'camp_report:read')
) as v (code, route, icon, sort_order, perm_code)
join iam.menu_item parent on parent.code = 'camps' and parent.corporation_id is null
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('camps.definitions', 'tr', 'Kamp Tanımları'),
    ('camps.definitions', 'en', 'Camp Definitions'),
    ('camps.enrollments', 'tr', 'Kamp Kayıtları'),
    ('camps.enrollments', 'en', 'Camp Enrollments'),
    ('camps.attendance',  'tr', 'Kamp Devam Takibi'),
    ('camps.attendance',  'en', 'Camp Attendance'),
    ('camps.reports',     'tr', 'Kamp Raporları'),
    ('camps.reports',     'en', 'Camp Reports')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
