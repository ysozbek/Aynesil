-- =============================================================================
-- AyNesil Platform :: Flyway V27 — Platform Admin Menu & Route Corrections
-- =============================================================================
-- Purely ADDITIVE where possible. Changes:
--   1. Fix V8 broken routes: payments→/finance, performance→/kpi
--   2. Deactivate V23's duplicate performance_kpi item (wrong route)
--   3. Add correct KPI sub-items under the fixed V8 'performance' item
--   4. Add Platform Administration section (group + 7 sub-items)
--   5. Add Parent Portal top-level item
--
-- UPDATE statements are idempotent (same value re-applied is a no-op).
-- INSERT statements use ON CONFLICT DO NOTHING (idempotent).
-- Owner rolüyle çalışır — RLS bypass.
-- =============================================================================


-- ── Step 1: Fix V8 broken route — payments → /finance ─────────────────────────

UPDATE iam.menu_item
SET route = '/finance'
WHERE code = 'payments'
  AND corporation_id IS NULL;

UPDATE iam.menu_item_translation
SET label = 'Finans'
WHERE locale = 'tr'
  AND menu_item_id = (
    SELECT id FROM iam.menu_item WHERE code = 'payments' AND corporation_id IS NULL
  );

UPDATE iam.menu_item_translation
SET label = 'Finance'
WHERE locale = 'en'
  AND menu_item_id = (
    SELECT id FROM iam.menu_item WHERE code = 'payments' AND corporation_id IS NULL
  );


-- ── Step 2: Fix V8 broken route — performance → /kpi ─────────────────────────

UPDATE iam.menu_item
SET route = '/kpi'
WHERE code = 'performance'
  AND corporation_id IS NULL;

UPDATE iam.menu_item_translation
SET label = 'KPI & Performans'
WHERE locale = 'tr'
  AND menu_item_id = (
    SELECT id FROM iam.menu_item WHERE code = 'performance' AND corporation_id IS NULL
  );

UPDATE iam.menu_item_translation
SET label = 'KPI & Performance'
WHERE locale = 'en'
  AND menu_item_id = (
    SELECT id FROM iam.menu_item WHERE code = 'performance' AND corporation_id IS NULL
  );


-- ── Step 3: Deactivate V23's duplicate performance_kpi (wrong route) ──────────
-- V23 inserted performance_kpi → /performance-kpi which doesn't match the Vue
-- router route /kpi. Deactivate to avoid duplicate broken sidebar entries.

UPDATE iam.menu_item
SET is_active = false
WHERE code = 'performance_kpi'
  AND corporation_id IS NULL;


-- ── Step 4: Add KPI sub-items under the fixed V8 'performance' item ───────────

INSERT INTO iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
SELECT
    null,
    parent.id,
    v.code,
    v.route,
    v.icon,
    v.sort_order,
    p.id,
    true
FROM (VALUES
    ('kpi.definitions', '/kpi/definitions', 'setting-2',   1, 'kpi:read'),
    ('kpi.snapshots',   '/kpi/snapshots',   'chart-line',  2, 'kpi_snapshot:read')
) AS v(code, route, icon, sort_order, perm_code)
JOIN iam.menu_item parent ON parent.code = 'performance' AND parent.corporation_id IS NULL
LEFT JOIN iam.permission p ON p.code = v.perm_code
ON CONFLICT DO NOTHING;

INSERT INTO iam.menu_item_translation (menu_item_id, locale, label)
SELECT m.id, t.locale, t.label
FROM iam.menu_item m
JOIN (VALUES
    ('kpi.definitions', 'tr', 'KPI Tanımları'),
    ('kpi.definitions', 'en', 'KPI Definitions'),
    ('kpi.snapshots',   'tr', 'Performans Anlık Görüntüleri'),
    ('kpi.snapshots',   'en', 'Performance Snapshots')
) AS t(code, locale, label) ON t.code = m.code AND m.corporation_id IS NULL
ON CONFLICT (menu_item_id, locale) DO NOTHING;


-- ── Step 5: Add Platform Administration group (top-level, sort 2) ─────────────

INSERT INTO iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
SELECT
    null,
    null,
    v.code,
    v.route,
    v.icon,
    v.sort_order,
    p.id,
    true
FROM (VALUES
    ('platform-admin', null, 'shield-tick', 2, 'corporation:read')
) AS v(code, route, icon, sort_order, perm_code)
LEFT JOIN iam.permission p ON p.code = v.perm_code
ON CONFLICT DO NOTHING;

INSERT INTO iam.menu_item_translation (menu_item_id, locale, label)
SELECT m.id, t.locale, t.label
FROM iam.menu_item m
JOIN (VALUES
    ('platform-admin', 'tr', 'Platform Yönetimi'),
    ('platform-admin', 'en', 'Platform Admin')
) AS t(code, locale, label) ON t.code = m.code AND m.corporation_id IS NULL
ON CONFLICT (menu_item_id, locale) DO NOTHING;


-- ── Step 6: Add Platform Admin sub-items ──────────────────────────────────────

INSERT INTO iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
SELECT
    null,
    parent.id,
    v.code,
    v.route,
    v.icon,
    v.sort_order,
    p.id,
    true
FROM (VALUES
    ('platform-admin.corporations',  '/corporations',   'building',      1, 'corporation:read'),
    ('platform-admin.campuses',      '/campuses',       'map',           2, 'campus:read'),
    ('platform-admin.users',         '/users',          'profile-2user', 3, 'user:read'),
    ('platform-admin.roles',         '/roles',          'shield-tick',   4, 'role:read'),
    ('platform-admin.permissions',   '/permissions',    'lock',          5, 'role:read'),
    ('platform-admin.menus',         '/menus',          'element-11',    6, 'menu:read'),
    ('platform-admin.reference-data','/reference-data', 'tag',           7, 'ref_data:read')
) AS v(code, route, icon, sort_order, perm_code)
JOIN iam.menu_item parent ON parent.code = 'platform-admin' AND parent.corporation_id IS NULL
LEFT JOIN iam.permission p ON p.code = v.perm_code
ON CONFLICT DO NOTHING;

INSERT INTO iam.menu_item_translation (menu_item_id, locale, label)
SELECT m.id, t.locale, t.label
FROM iam.menu_item m
JOIN (VALUES
    ('platform-admin.corporations',   'tr', 'Kurumlar'),
    ('platform-admin.corporations',   'en', 'Corporations'),
    ('platform-admin.campuses',       'tr', 'Kampüsler'),
    ('platform-admin.campuses',       'en', 'Campuses'),
    ('platform-admin.users',          'tr', 'Kullanıcılar'),
    ('platform-admin.users',          'en', 'Users'),
    ('platform-admin.roles',          'tr', 'Roller'),
    ('platform-admin.roles',          'en', 'Roles'),
    ('platform-admin.permissions',    'tr', 'İzinler'),
    ('platform-admin.permissions',    'en', 'Permissions'),
    ('platform-admin.menus',          'tr', 'Menü Yönetimi'),
    ('platform-admin.menus',          'en', 'Menu Management'),
    ('platform-admin.reference-data', 'tr', 'Referans Veriler'),
    ('platform-admin.reference-data', 'en', 'Reference Data')
) AS t(code, locale, label) ON t.code = m.code AND m.corporation_id IS NULL
ON CONFLICT (menu_item_id, locale) DO NOTHING;


-- ── Step 7: Add Parent Portal top-level item ──────────────────────────────────
-- Portal access is granted to guardian accounts (portal:access permission).
-- Sort 175 — after settings (160) and notifications (165 from V17).

INSERT INTO iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
SELECT
    null,
    null,
    v.code,
    v.route,
    v.icon,
    v.sort_order,
    p.id,
    true
FROM (VALUES
    ('portal', '/portal', 'home-2', 175, 'portal:access')
) AS v(code, route, icon, sort_order, perm_code)
LEFT JOIN iam.permission p ON p.code = v.perm_code
ON CONFLICT DO NOTHING;

INSERT INTO iam.menu_item_translation (menu_item_id, locale, label)
SELECT m.id, t.locale, t.label
FROM iam.menu_item m
JOIN (VALUES
    ('portal', 'tr', 'Veli Portalı'),
    ('portal', 'en', 'Parent Portal')
) AS t(code, locale, label) ON t.code = m.code AND m.corporation_id IS NULL
ON CONFLICT (menu_item_id, locale) DO NOTHING;


-- ── Step 8: Grant all new platform-admin permissions to admin role ─────────────

INSERT INTO iam.role_permission (role_id, permission_id)
SELECT r.id, p.id
FROM iam.role r
CROSS JOIN iam.permission p
WHERE r.code = 'admin'
  AND p.code IN (
    'corporation:read', 'corporation:create', 'corporation:update', 'corporation:delete',
    'campus:read',      'campus:create',      'campus:update',      'campus:delete',
    'user:read',        'user:create',        'user:update',        'user:delete',
    'user:reset_password',
    'role:read',        'role:create',        'role:update',        'role:delete',
    'role:assign_permission',
    'menu:read',        'menu:manage',
    'ref_data:read',    'ref_data:manage',
    'settings:read',    'settings:manage',
    'portal:access'
  )
ON CONFLICT DO NOTHING;
