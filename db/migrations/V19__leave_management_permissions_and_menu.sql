-- =============================================================================
-- AyNesil Platform :: Flyway V19 — Leave Management Permissions & Menu
-- =============================================================================
-- Purely ADDITIVE. Objects created / seeded:
--   1. Additional leave_type ref_values and their translations (tr + en)
--      (annual, sick, unpaid, hourly already seeded in V2/baseline seed)
--   2. Translations for baseline leave_type ref_values (were missing)
--   3. Permissions: leave_request:*, leave_balance:*, leave_report:read
--   4. Menu items: Leave top-level + sub-items
--   5. Role grants: all new permissions → admin role
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================


-- ── Step 1: Additional leave_type ref_values ──────────────────────────────────
-- Baseline seed has: annual, sick, unpaid, hourly.
-- Adding: medical, excuse, maternity, paternity, bereavement.

insert into ref.ref_value (ref_type_id, code, sort_order, is_default, is_active)
select ref.type_id(t.type_code), t.code, t.sort_order, false, true
from (values
  ('leave_type', 'medical',      5),
  ('leave_type', 'excuse',       6),
  ('leave_type', 'maternity',    7),
  ('leave_type', 'paternity',    8),
  ('leave_type', 'bereavement',  9)
) as t (type_code, code, sort_order)
where ref.type_id(t.type_code) is not null
on conflict do nothing;


-- ── Step 2: Translations for all leave_type ref_values ────────────────────────
-- Includes baseline values (annual, sick, unpaid, hourly) and new values above.

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  ('leave_type', 'annual',      'tr', 'Yıllık İzin'),
  ('leave_type', 'annual',      'en', 'Annual Leave'),
  ('leave_type', 'sick',        'tr', 'Hastalık İzni'),
  ('leave_type', 'sick',        'en', 'Sick Leave'),
  ('leave_type', 'unpaid',      'tr', 'Ücretsiz İzin'),
  ('leave_type', 'unpaid',      'en', 'Unpaid Leave'),
  ('leave_type', 'hourly',      'tr', 'Saatlik İzin'),
  ('leave_type', 'hourly',      'en', 'Hourly Leave'),
  ('leave_type', 'medical',     'tr', 'Sağlık İzni'),
  ('leave_type', 'medical',     'en', 'Medical Leave'),
  ('leave_type', 'excuse',      'tr', 'Mazeret İzni'),
  ('leave_type', 'excuse',      'en', 'Excuse Leave'),
  ('leave_type', 'maternity',   'tr', 'Doğum İzni (Anne)'),
  ('leave_type', 'maternity',   'en', 'Maternity Leave'),
  ('leave_type', 'paternity',   'tr', 'Doğum İzni (Baba)'),
  ('leave_type', 'paternity',   'en', 'Paternity Leave'),
  ('leave_type', 'bereavement', 'tr', 'Yas İzni'),
  ('leave_type', 'bereavement', 'en', 'Bereavement Leave')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code        = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 3: Permission catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  ('leave_request:read',    'leave_request', 'read'),
  ('leave_request:submit',  'leave_request', 'submit'),
  ('leave_request:update',  'leave_request', 'update'),
  ('leave_request:cancel',  'leave_request', 'cancel'),
  ('leave_request:approve', 'leave_request', 'approve'),
  ('leave_request:reject',  'leave_request', 'reject'),
  ('leave_balance:read',    'leave_balance', 'read'),
  ('leave_balance:manage',  'leave_balance', 'manage'),
  ('leave_report:read',     'leave_report',  'read')
on conflict (code) do nothing;


-- ── Step 4: Menu items ────────────────────────────────────────────────────────

-- Leave top-level menu item
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
    ('leave', '/leave', 'calendar-days', 90, 'leave_request:read')
) as v (code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('leave', 'tr', 'İzin Yönetimi'),
    ('leave', 'en', 'Leave Management')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- Leave sub-items
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
    ('leave-requests',  '/leave/requests',         'list',          10, 'leave_request:read'),
    ('leave-calendar',  '/leave/calendar',          'calendar',      20, 'leave_request:read'),
    ('leave-balances',  '/leave/balances',          'chart-bar',     30, 'leave_balance:read'),
    ('leave-reports',   '/leave/reports',           'document-chart-bar', 40, 'leave_report:read')
) as v (code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'leave' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('leave-requests', 'tr', 'İzin Talepleri'),
    ('leave-requests', 'en', 'Leave Requests'),
    ('leave-calendar', 'tr', 'İzin Takvimi'),
    ('leave-calendar', 'en', 'Leave Calendar'),
    ('leave-balances', 'tr', 'İzin Bakiyeleri'),
    ('leave-balances', 'en', 'Leave Balances'),
    ('leave-reports',  'tr', 'İzin Raporları'),
    ('leave-reports',  'en', 'Leave Reports')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;


-- ── Step 5: Grant all new permissions to the admin role ───────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where p.code in (
  'leave_request:read',  'leave_request:submit', 'leave_request:update',
  'leave_request:cancel','leave_request:approve','leave_request:reject',
  'leave_balance:read',  'leave_balance:manage',
  'leave_report:read'
)
  and r.name = 'admin'
on conflict do nothing;
