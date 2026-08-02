-- =============================================================================
-- AyNesil Platform :: Flyway V23 — Educator Performance & KPI
-- =============================================================================
-- Purely ADDITIVE. Objects created / seeded:
--   1. ALTER core.kpi_definition ADD COLUMN is_active (was missing from baseline DDL)
--   2. ref_values for kpi_category: session_performance, attendance_performance,
--      goal_performance, parent_satisfaction, utilization, program_completion
--   3. Platform kpi_definition records (six built-in educator KPI metrics)
--   4. Permissions: kpi:*, kpi_snapshot:*, parent_feedback:*, kpi_report:*, kpi_dashboard:*
--   5. Role grants: admin
--   6. Menu items: Performance & KPI top-level + sub-items, tr/en translations
--
-- Idempotent (ON CONFLICT DO NOTHING, ADD COLUMN IF NOT EXISTS).
-- Runs as owner role — RLS bypass.
-- ref_type 'kpi_category' already seeded in 01_reference_data_seed.sql.
-- =============================================================================


-- ── Step 1: Extend kpi_definition with is_active ──────────────────────────────

alter table core.kpi_definition
    add column if not exists is_active boolean not null default true;


-- ── Step 2: Seed kpi_category values ─────────────────────────────────────────

insert into ref.ref_value (ref_type_id, code, sort_order, is_active, is_system)
select ref.type_id('kpi_category'), v.code, v.sort, true, true
from (values
    ('session_performance',    1),
    ('attendance_performance', 2),
    ('goal_performance',       3),
    ('parent_satisfaction',    4),
    ('utilization',            5),
    ('program_completion',     6)
) as v (code, sort)
on conflict do nothing;


-- ── Step 3: Translations for kpi_category values ─────────────────────────────

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from ref.ref_value rv
join ref.ref_type rt on rt.id = rv.ref_type_id and rt.code = 'kpi_category'
join (values
    ('session_performance',    'tr', 'Seans Performansı'),
    ('session_performance',    'en', 'Session Performance'),
    ('attendance_performance', 'tr', 'Devam Performansı'),
    ('attendance_performance', 'en', 'Attendance Performance'),
    ('goal_performance',       'tr', 'Hedef Performansı'),
    ('goal_performance',       'en', 'Goal Performance'),
    ('parent_satisfaction',    'tr', 'Veli Memnuniyeti'),
    ('parent_satisfaction',    'en', 'Parent Satisfaction'),
    ('utilization',            'tr', 'Kullanım Oranı'),
    ('utilization',            'en', 'Utilization Rate'),
    ('program_completion',     'tr', 'Program Tamamlama'),
    ('program_completion',     'en', 'Program Completion')
) as t (code, locale, label) on t.code = rv.code
where rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 4: Seed platform KPI definitions ─────────────────────────────────────

insert into core.kpi_definition (corporation_id, code, name, category_id, unit, spec)
select
    null,
    v.code,
    v.name,
    (select rv.id
       from ref.ref_value rv
       join ref.ref_type rt on rt.id = rv.ref_type_id
      where rt.code = 'kpi_category'
        and rv.code = v.cat
        and rv.corporation_id is null),
    v.unit,
    v.spec::jsonb
from (values
    ('educator.session_volume',      'Session Volume',            'session_performance',    'count',  '{"metric":"session_count","aggregation":"sum","subject_type":"educator"}'),
    ('educator.attendance_rate',     'Attendance Rate',           'attendance_performance', '%',      '{"metric":"attendance_rate","aggregation":"avg","subject_type":"educator"}'),
    ('educator.goal_achievement',    'Goal Achievement Rate',     'goal_performance',       '%',      '{"metric":"goal_achievement_rate","aggregation":"avg","subject_type":"educator"}'),
    ('educator.parent_satisfaction', 'Parent Satisfaction Score', 'parent_satisfaction',    'rating', '{"metric":"parent_feedback_avg","aggregation":"avg","subject_type":"educator","scale":5}'),
    ('educator.utilization_rate',    'Utilization Rate',          'utilization',            '%',      '{"metric":"utilization_rate","aggregation":"avg","subject_type":"educator"}'),
    ('educator.program_completion',  'Program Completion Rate',   'program_completion',     '%',      '{"metric":"program_completion_rate","aggregation":"avg","subject_type":"educator"}')
) as v (code, name, cat, unit, spec)
on conflict do nothing;


-- ── Step 5: Permission catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action, description) values
    ('kpi:read',              'kpi',             'read',    'View KPI definitions and categories'),
    ('kpi:manage',            'kpi',             'manage',  'Create and update KPI definitions'),
    ('kpi:compute',           'kpi',             'compute', 'Trigger KPI computation for educators'),
    ('kpi_snapshot:read',     'kpi_snapshot',    'read',    'View educator performance snapshots'),
    ('kpi_snapshot:manage',   'kpi_snapshot',    'manage',  'Create and refresh educator performance snapshots'),
    ('parent_feedback:read',  'parent_feedback', 'read',    'View parent feedback ratings for educators'),
    ('parent_feedback:submit','parent_feedback',  'submit',  'Submit a parent feedback rating for a session'),
    ('kpi_report:read',       'kpi_report',      'read',    'View KPI performance and trend reports'),
    ('kpi_dashboard:read',    'kpi_dashboard',   'read',    'Access educator, manager, and executive KPI dashboards')
on conflict (code) do nothing;


-- ── Step 6: Role grants (admin role gets all KPI permissions) ─────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.role r
cross join iam.permission p
where r.code = 'admin'
  and p.code in (
    'kpi:read', 'kpi:manage', 'kpi:compute',
    'kpi_snapshot:read', 'kpi_snapshot:manage',
    'parent_feedback:read', 'parent_feedback:submit',
    'kpi_report:read', 'kpi_dashboard:read'
  )
on conflict do nothing;


-- ── Step 7: Menu items (platform default — corporation_id NULL) ────────────────

insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select null, null, v.code, v.route, v.icon, v.sort_order, p.id, true
from (values
    ('performance_kpi', '/performance-kpi', 'trending-up', 60, 'kpi_dashboard:read')
) as v (code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('performance_kpi', 'tr', 'Performans & KPI'),
    ('performance_kpi', 'en', 'Performance & KPI')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- Sub-items under performance_kpi
insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select null, parent.id, v.code, v.route, v.icon, v.sort_order, p.id, true
from (values
    ('performance_kpi.definitions', '/performance-kpi/definitions', 'settings',    1, 'kpi:read'),
    ('performance_kpi.educator',    '/performance-kpi/educator',    'user',         2, 'kpi_dashboard:read'),
    ('performance_kpi.manager',     '/performance-kpi/manager',     'users',        3, 'kpi_dashboard:read'),
    ('performance_kpi.executive',   '/performance-kpi/executive',   'bar-chart-2',  4, 'kpi_dashboard:read'),
    ('performance_kpi.reports',     '/performance-kpi/reports',     'file-text',    5, 'kpi_report:read')
) as v (code, route, icon, sort_order, perm_code)
join iam.menu_item parent on parent.code = 'performance_kpi' and parent.corporation_id is null
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('performance_kpi.definitions', 'tr', 'KPI Tanımları'),
    ('performance_kpi.definitions', 'en', 'KPI Definitions'),
    ('performance_kpi.educator',    'tr', 'Eğitmen Paneli'),
    ('performance_kpi.educator',    'en', 'Educator Dashboard'),
    ('performance_kpi.manager',     'tr', 'Yönetici Paneli'),
    ('performance_kpi.manager',     'en', 'Manager Dashboard'),
    ('performance_kpi.executive',   'tr', 'Yönetim Paneli'),
    ('performance_kpi.executive',   'en', 'Executive Dashboard'),
    ('performance_kpi.reports',     'tr', 'Performans Raporları'),
    ('performance_kpi.reports',     'en', 'Performance Reports')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
