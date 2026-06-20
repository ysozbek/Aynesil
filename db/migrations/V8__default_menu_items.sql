-- =============================================================================
-- AyNesil Platform :: Flyway V8 — Default Platform Menu Items
-- =============================================================================
-- Seeds the 16 platform-level top-level navigation items (corporation_id = NULL).
-- Platform items are visible across all tenants and cannot be deleted via the API
-- (MenuItem.EnsureCanBeDeleted guard). Tenants may deactivate items they do not
-- need and create tenant-scoped custom items or sub-menus beneath them.
--
-- required_permission_id is resolved at insert time via LEFT JOIN on iam.permission.
-- Items without a matching permission code get required_permission_id = NULL,
-- meaning they are visible to all authenticated users.
--
-- Idempotent (ON CONFLICT DO NOTHING on the unique nulls not distinct
-- (corporation_id, code) index). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================

-- ── Step 1: Root-level platform menu items ───────────────────────────────────

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
    -- code                  route                icon               sort  perm_code
    ('dashboard',         '/dashboard',        'element-11',        10,   null),
    ('crm',               '/crm',              'profile-2user',     20,   'student:read'),
    ('assessment',        '/assessment',       'clipboard-text',    30,   'student:read'),
    ('students',          '/students',         'profile-user',      40,   'student:read'),
    ('educators',         '/educators',        'teacher',           50,   'user:read'),
    ('scheduling',        '/scheduling',       'calendar-2',        60,   'session:read'),
    ('bep',               '/bep',              'book-open',         70,   'student:read'),
    ('payments',          '/payments',         'wallet',            80,   'finance:read'),
    ('meetings',          '/meetings',         'messages',          90,   null),
    ('leave-management',  '/leave',            'timer',             100,  null),
    ('camera-management', '/cameras',          'camera',            110,  null),
    ('camp-management',   '/camps',            'flag',              120,  null),
    ('consultancy',       '/consultancy',      'people',            130,  null),
    ('performance',       '/performance',      'chart-line',        140,  'report:read'),
    ('reports',           '/reports',          'chart-pie',         150,  'report:read'),
    ('settings',          '/settings',         'setting-2',         160,  'settings:read')
) as v(code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 2: Turkish and English translations ──────────────────────────────────

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    -- Dashboard
    ('dashboard',         'tr', 'Ana Sayfa'),
    ('dashboard',         'en', 'Dashboard'),

    -- CRM (prospect & family management)
    ('crm',               'tr', 'CRM'),
    ('crm',               'en', 'CRM'),

    -- Assessment
    ('assessment',        'tr', 'Değerlendirme'),
    ('assessment',        'en', 'Assessment'),

    -- Students
    ('students',          'tr', 'Öğrenciler'),
    ('students',          'en', 'Students'),

    -- Educators
    ('educators',         'tr', 'Eğitimciler'),
    ('educators',         'en', 'Educators'),

    -- Scheduling
    ('scheduling',        'tr', 'Seans Planlama'),
    ('scheduling',        'en', 'Scheduling'),

    -- BEP — Bireysel Eğitim Programı / Individual Education Plan
    ('bep',               'tr', 'BEP'),
    ('bep',               'en', 'IEP'),

    -- Payments
    ('payments',          'tr', 'Ödemeler'),
    ('payments',          'en', 'Payments'),

    -- Meetings
    ('meetings',          'tr', 'Toplantılar'),
    ('meetings',          'en', 'Meetings'),

    -- Leave Management
    ('leave-management',  'tr', 'İzin Yönetimi'),
    ('leave-management',  'en', 'Leave Management'),

    -- Camera Management
    ('camera-management', 'tr', 'Kamera Yönetimi'),
    ('camera-management', 'en', 'Camera Management'),

    -- Camp Management
    ('camp-management',   'tr', 'Kamp Yönetimi'),
    ('camp-management',   'en', 'Camp Management'),

    -- Consultancy
    ('consultancy',       'tr', 'Danışmanlık'),
    ('consultancy',       'en', 'Consultancy'),

    -- Performance
    ('performance',       'tr', 'Performans'),
    ('performance',       'en', 'Performance'),

    -- Reports
    ('reports',           'tr', 'Raporlar'),
    ('reports',           'en', 'Reports'),

    -- Settings
    ('settings',          'tr', 'Ayarlar'),
    ('settings',          'en', 'Settings')
) as t(code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
