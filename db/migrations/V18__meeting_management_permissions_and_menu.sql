-- =============================================================================
-- AyNesil Platform :: Flyway V18 — Meeting Management Permissions & Menu
-- =============================================================================
-- Purely ADDITIVE. Objects created / seeded:
--   1. Translations (tr + en) for meeting_type ref_values seeded in V2
--      (translations were missing from the baseline seed)
--   2. Permissions: meeting:read, meeting:create, meeting:update, meeting:delete,
--      meeting:complete, meeting:cancel, meeting:manage_participants,
--      meeting:record_attendance, meeting:record_outcome, meeting:manage_follow_ups
--   3. Menu items: Meetings top-level + sub-items (list, calendar)
--   4. Role grants: all new permissions → admin role
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================


-- ── Step 1: Translations for meeting_type ref_values ─────────────────────────
-- V2 seeded the values (internal, parent, prospect, external) but omitted translations.

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  ('meeting_type', 'internal',  'tr', 'İç Toplantı'),
  ('meeting_type', 'internal',  'en', 'Internal Meeting'),
  ('meeting_type', 'parent',    'tr', 'Veli Toplantısı'),
  ('meeting_type', 'parent',    'en', 'Parent Meeting'),
  ('meeting_type', 'prospect',  'tr', 'Aday Toplantısı'),
  ('meeting_type', 'prospect',  'en', 'Prospect Meeting'),
  ('meeting_type', 'external',  'tr', 'Dış Toplantı'),
  ('meeting_type', 'external',  'en', 'External Meeting')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code        = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 2: Permission catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  ('meeting:read',               'meeting', 'read'),
  ('meeting:create',             'meeting', 'create'),
  ('meeting:update',             'meeting', 'update'),
  ('meeting:delete',             'meeting', 'delete'),
  ('meeting:complete',           'meeting', 'complete'),
  ('meeting:cancel',             'meeting', 'cancel'),
  ('meeting:manage_participants','meeting', 'manage_participants'),
  ('meeting:record_attendance',  'meeting', 'record_attendance'),
  ('meeting:record_outcome',     'meeting', 'record_outcome'),
  ('meeting:manage_follow_ups',  'meeting', 'manage_follow_ups')
on conflict (code) do nothing;


-- ── Step 3: Menu items ────────────────────────────────────────────────────────

-- Meetings top-level menu item
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
    ('meetings', '/meetings', 'users', 80, 'meeting:read')
) as v (code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('meetings', 'tr', 'Toplantılar'),
    ('meetings', 'en', 'Meetings')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- Meetings sub-items
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
    ('meetings-list',     '/meetings',          'list',         10, 'meeting:read'),
    ('meetings-calendar', '/meetings/calendar', 'calendar',     20, 'meeting:read'),
    ('meetings-follow-ups','/meetings/follow-ups','check-square',30,'meeting:manage_follow_ups')
) as v (code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'meetings' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('meetings-list',      'tr', 'Tüm Toplantılar'),
    ('meetings-list',      'en', 'All Meetings'),
    ('meetings-calendar',  'tr', 'Takvim'),
    ('meetings-calendar',  'en', 'Calendar'),
    ('meetings-follow-ups','tr', 'Takip Maddeleri'),
    ('meetings-follow-ups','en', 'Follow-ups')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;


-- ── Step 4: Grant all new permissions to the admin role ───────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where p.code in (
  'meeting:read', 'meeting:create', 'meeting:update', 'meeting:delete',
  'meeting:complete', 'meeting:cancel',
  'meeting:manage_participants', 'meeting:record_attendance',
  'meeting:record_outcome', 'meeting:manage_follow_ups'
)
  and r.name = 'admin'
on conflict do nothing;
