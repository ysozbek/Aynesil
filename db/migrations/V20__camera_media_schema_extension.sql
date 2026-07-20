-- =============================================================================
-- AyNesil Platform :: Flyway V20 — Camera & Live Session Schema Extension
-- =============================================================================
-- ADDITIVE + backward-compatible. Objects created / seeded:
--   1. ref_type:  camera_type, access_type
--   2. ref_value: fixed/ptz/mobile (camera_type); live_only/live_replay/replay_only (access_type)
--   3. ref_value_translation: tr + en for all new values
--   4. ALTER media.camera             ADD COLUMN camera_type_id
--   5. ALTER media.viewing_authorization ADD COLUMN access_type_id
--   6. Permissions: camera:*, camera_assignment:*, viewing_authorization:*,
--                   viewing_session:*, viewing_log:read
--   7. Menu items: Camera Management top-level + sub-items (tr + en)
--   8. Role grants: all new permissions → admin role
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================


-- ── Step 1: ref types ─────────────────────────────────────────────────────────

insert into ref.ref_type (code, name, is_system, is_hierarchical, allows_tenant_values) values
  ('camera_type', 'Camera Types',        false, false, true),
  ('access_type', 'Camera Access Types', false, false, true)
on conflict (code) do nothing;


-- ── Step 2: ref values ────────────────────────────────────────────────────────
-- uq_ref_value_one_default: only ONE is_default=true per (ref_type_id, corporation_id).

insert into ref.ref_value (ref_type_id, code, sort_order, is_default, is_active)
select ref.type_id(t.type_code), t.code, t.sort_order, t.is_default, true
from (values
  ('camera_type', 'fixed',       1, true),
  ('camera_type', 'ptz',         2, false),
  ('camera_type', 'mobile',      3, false),
  ('access_type', 'live_only',   1, true),
  ('access_type', 'live_replay', 2, false),
  ('access_type', 'replay_only', 3, false)
) as t (type_code, code, sort_order, is_default)
where ref.type_id(t.type_code) is not null
on conflict (ref_type_id, corporation_id, code) do nothing;


-- ── Step 3: ref value translations (tr + en) ──────────────────────────────────

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  ('camera_type', 'fixed',       'tr', 'Sabit Kamera'),
  ('camera_type', 'fixed',       'en', 'Fixed Camera'),
  ('camera_type', 'ptz',         'tr', 'PTZ Kamera'),
  ('camera_type', 'ptz',         'en', 'PTZ Camera'),
  ('camera_type', 'mobile',      'tr', 'Mobil Kamera'),
  ('camera_type', 'mobile',      'en', 'Mobile Camera'),
  ('access_type', 'live_only',   'tr', 'Sadece Canlı'),
  ('access_type', 'live_only',   'en', 'Live Only'),
  ('access_type', 'live_replay', 'tr', 'Canlı + Tekrar İzleme'),
  ('access_type', 'live_replay', 'en', 'Live + Replay'),
  ('access_type', 'replay_only', 'tr', 'Sadece Tekrar İzleme'),
  ('access_type', 'replay_only', 'en', 'Replay Only')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id  = ref.type_id(t.type_code)
 and rv.code         = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 4: schema extension (nullable FKs — fully backward-compatible) ────────

alter table media.camera
  add column if not exists camera_type_id uuid references ref.ref_value(id);

alter table media.viewing_authorization
  add column if not exists access_type_id uuid references ref.ref_value(id);


-- ── Step 5: permission catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  ('camera:read',                  'camera',                'read'),
  ('camera:create',                'camera',                'create'),
  ('camera:update',                'camera',                'update'),
  ('camera:delete',                'camera',                'delete'),
  ('camera_assignment:read',       'camera_assignment',     'read'),
  ('camera_assignment:manage',     'camera_assignment',     'manage'),
  ('viewing_authorization:read',   'viewing_authorization', 'read'),
  ('viewing_authorization:grant',  'viewing_authorization', 'grant'),
  ('viewing_authorization:revoke', 'viewing_authorization', 'revoke'),
  ('viewing_session:start',        'viewing_session',       'start'),
  ('viewing_session:end',          'viewing_session',       'end'),
  ('viewing_log:read',             'viewing_log',           'read')
on conflict (code) do nothing;


-- ── Step 6: menu items ────────────────────────────────────────────────────────

-- Top-level: Camera Management
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
    ('cameras', '/cameras', 'video-camera', 95, 'camera:read')
) as v (code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('cameras', 'tr', 'Kamera Yönetimi'),
    ('cameras', 'en', 'Camera Management')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- Sub-items
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
    ('camera-list',           '/cameras/list',           'list',   10, 'camera:read'),
    ('camera-authorizations', '/cameras/authorizations', 'shield', 20, 'viewing_authorization:read'),
    ('camera-viewing-logs',   '/cameras/viewing-logs',   'clock',  30, 'viewing_log:read')
) as v (code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'cameras' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('camera-list',           'tr', 'Kameralar'),
    ('camera-list',           'en', 'Cameras'),
    ('camera-authorizations', 'tr', 'İzleme Yetkileri'),
    ('camera-authorizations', 'en', 'Viewing Authorizations'),
    ('camera-viewing-logs',   'tr', 'İzleme Kayıtları'),
    ('camera-viewing-logs',   'en', 'Viewing Logs')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;


-- ── Step 7: grant all new permissions to the admin role ───────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where p.code in (
  'camera:read',                  'camera:create',
  'camera:update',                'camera:delete',
  'camera_assignment:read',       'camera_assignment:manage',
  'viewing_authorization:read',   'viewing_authorization:grant',
  'viewing_authorization:revoke',
  'viewing_session:start',        'viewing_session:end',
  'viewing_log:read'
)
  and r.name = 'admin'
on conflict do nothing;
