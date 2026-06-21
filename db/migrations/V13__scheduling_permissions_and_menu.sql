-- =============================================================================
-- AyNesil Platform :: Flyway V13 — Session & Scheduling Permissions + Menu
-- =============================================================================
-- Adds permission codes for Rooms, Sessions, Session Notes, Attendance,
-- and Makeup Requests.
-- Seeds top-level + sub-navigation menu items with tr/en translations.
-- Grants all new permissions to the default admin role.
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================

-- ── Step 1: Permission Catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  -- Rooms
  ('room:read',                    'room',            'read'),
  ('room:create',                  'room',            'create'),
  ('room:update',                  'room',            'update'),
  ('room:delete',                  'room',            'delete'),
  -- Sessions
  ('session:read',                 'session',         'read'),
  ('session:create',               'session',         'create'),
  ('session:update',               'session',         'update'),
  ('session:delete',               'session',         'delete'),
  ('session:reschedule',           'session',         'reschedule'),
  ('session:complete',             'session',         'complete'),
  ('session:cancel',               'session',         'cancel'),
  ('session:manage_participants',  'session',         'manage_participants'),
  ('session:manage_educators',     'session',         'manage_educators'),
  ('session:manage_goals',         'session',         'manage_goals'),
  ('session:manage_calendar',      'session',         'manage_calendar'),
  ('session:bulk_generate',        'session',         'bulk_generate'),
  ('session:bulk_cancel',          'session',         'bulk_cancel'),
  ('session:bulk_reassign',        'session',         'bulk_reassign'),
  -- Session Notes
  ('session_note:write',           'session_note',    'write'),
  ('session_note:delete',          'session_note',    'delete'),
  -- Attendance
  ('attendance:read',              'attendance',      'read'),
  ('attendance:record',            'attendance',      'record'),
  -- Makeup Requests
  ('makeup_request:read',          'makeup_request',  'read'),
  ('makeup_request:request',       'makeup_request',  'request'),
  ('makeup_request:manage',        'makeup_request',  'manage')
on conflict (code) do nothing;

-- ── Step 2: Top-level Menu Item ───────────────────────────────────────────────

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
    ('scheduling', '/scheduling', 'calendar', 60, 'session:read')
) as v(code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 3: Scheduling Sub-menu Items ─────────────────────────────────────────

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
    ('scheduling-sessions',        '/scheduling/sessions',         'calendar-days',  10, 'session:read'),
    ('scheduling-recurring',       '/scheduling/recurring',        'refresh',        20, 'session:read'),
    ('scheduling-rooms',           '/scheduling/rooms',            'building-office',30, 'room:read'),
    ('scheduling-attendance',      '/scheduling/attendance',       'clipboard-list', 40, 'attendance:read'),
    ('scheduling-makeup',          '/scheduling/makeup',           'arrow-path',     50, 'makeup_request:read'),
    ('scheduling-calendar',        '/scheduling/calendar',         'calendar',       60, 'session:read')
) as v(code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'scheduling' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 4: Translations (tr + en) ───────────────────────────────────────────

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('scheduling',              'tr', 'Seans & Planlama'),
    ('scheduling',              'en', 'Scheduling'),
    ('scheduling-sessions',     'tr', 'Seanslar'),
    ('scheduling-sessions',     'en', 'Sessions'),
    ('scheduling-recurring',    'tr', 'Tekrarlayan Programlar'),
    ('scheduling-recurring',    'en', 'Recurring Schedules'),
    ('scheduling-rooms',        'tr', 'Odalar'),
    ('scheduling-rooms',        'en', 'Rooms'),
    ('scheduling-attendance',   'tr', 'Yoklama'),
    ('scheduling-attendance',   'en', 'Attendance'),
    ('scheduling-makeup',       'tr', 'Telafi Seansları'),
    ('scheduling-makeup',       'en', 'Makeup Sessions'),
    ('scheduling-calendar',     'tr', 'Takvim'),
    ('scheduling-calendar',     'en', 'Calendar')
) as t(code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- ── Step 5: Grant all new permissions to default admin role ───────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where p.code in (
    'room:read','room:create','room:update','room:delete',
    'session:read','session:create','session:update','session:delete',
    'session:reschedule','session:complete','session:cancel',
    'session:manage_participants','session:manage_educators',
    'session:manage_goals','session:manage_calendar',
    'session:bulk_generate','session:bulk_cancel','session:bulk_reassign',
    'session_note:write','session_note:delete',
    'attendance:read','attendance:record',
    'makeup_request:read','makeup_request:request','makeup_request:manage'
)
  and r.name = 'admin'
on conflict do nothing;
