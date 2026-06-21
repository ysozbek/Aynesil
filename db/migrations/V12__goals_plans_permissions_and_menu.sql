-- =============================================================================
-- AyNesil Platform :: Flyway V12 — BEP/IEP & Goal Management Permissions + Menu
-- =============================================================================
-- Adds permission codes for Goal Libraries, Goal Templates, Student Goals,
-- Goal Progress, Academic Periods, Education Plans (BEP/IEP), and Goal Reports.
-- Seeds top-level + sub-navigation menu items with tr/en translations.
-- Grants all new permissions to the default admin role.
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================

-- ── Step 1: Permission Catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  -- Goal Library
  ('goal_library:read',            'goal_library',    'read'),
  ('goal_library:create',          'goal_library',    'create'),
  ('goal_library:update',          'goal_library',    'update'),
  ('goal_library:delete',          'goal_library',    'delete'),
  -- Goal Templates
  ('goal_template:read',           'goal_template',   'read'),
  ('goal_template:create',         'goal_template',   'create'),
  ('goal_template:update',         'goal_template',   'update'),
  ('goal_template:delete',         'goal_template',   'delete'),
  ('goal_template:translate',      'goal_template',   'translate'),
  -- Student Goals
  ('student_goal:read',            'student_goal',    'read'),
  ('student_goal:create',          'student_goal',    'create'),
  ('student_goal:update',          'student_goal',    'update'),
  ('student_goal:delete',          'student_goal',    'delete'),
  ('student_goal:change_status',   'student_goal',    'change_status'),
  -- Goal Progress
  ('goal_progress:read',           'goal_progress',   'read'),
  ('goal_progress:record',         'goal_progress',   'record'),
  -- Academic Periods
  ('academic_period:read',         'academic_period', 'read'),
  ('academic_period:manage',       'academic_period', 'manage'),
  -- Education Plans (BEP/IEP)
  ('education_plan:read',          'education_plan',  'read'),
  ('education_plan:create',        'education_plan',  'create'),
  ('education_plan:update',        'education_plan',  'update'),
  ('education_plan:delete',        'education_plan',  'delete'),
  ('education_plan:submit',        'education_plan',  'submit'),
  ('education_plan:approve',       'education_plan',  'approve'),
  ('education_plan:revise',        'education_plan',  'revise'),
  ('education_plan:manage_goals',  'education_plan',  'manage_goals'),
  ('education_plan:add_review',    'education_plan',  'add_review'),
  ('education_plan:guardian_view', 'education_plan',  'guardian_view'),
  -- Goal Reports
  ('goal_report:read',             'goal_report',     'read'),
  ('goal_report:export',           'goal_report',     'export')
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
    ('goals',          '/goals',          'flag',         50, 'student_goal:read'),
    ('education-plans','/education-plans','document-text', 55, 'education_plan:read')
) as v(code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 3: Goals Sub-menu Items ──────────────────────────────────────────────

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
    ('goals-library',       '/goals/libraries',        'library',    10, 'goal_library:read'),
    ('goals-templates',     '/goals/templates',        'template',   20, 'goal_template:read'),
    ('goals-student',       '/goals/student-goals',    'flag',       30, 'student_goal:read'),
    ('goals-analytics',     '/goals/analytics',        'chart-bar',  40, 'goal_report:read')
) as v(code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'goals' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 4: Education Plans Sub-menu Items ────────────────────────────────────

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
    ('plans-list',          '/education-plans',              'document-text', 10, 'education_plan:read'),
    ('plans-academic',      '/education-plans/academic-periods','calendar',   20, 'academic_period:read'),
    ('plans-reports',       '/education-plans/reports',      'chart-pie',     30, 'goal_report:read'),
    ('plans-new',           '/education-plans/new',          'add-circle',    40, 'education_plan:create')
) as v(code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'education-plans' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 5: Translations (tr + en) ───────────────────────────────────────────

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('goals',               'tr', 'Hedefler'),
    ('goals',               'en', 'Goals'),
    ('goals-library',       'tr', 'Hedef Kütüphanesi'),
    ('goals-library',       'en', 'Goal Library'),
    ('goals-templates',     'tr', 'Hedef Şablonları'),
    ('goals-templates',     'en', 'Goal Templates'),
    ('goals-student',       'tr', 'Öğrenci Hedefleri'),
    ('goals-student',       'en', 'Student Goals'),
    ('goals-analytics',     'tr', 'Hedef Analitiği'),
    ('goals-analytics',     'en', 'Goal Analytics'),
    ('education-plans',     'tr', 'BEP / IEP'),
    ('education-plans',     'en', 'Education Plans'),
    ('plans-list',          'tr', 'Plan Listesi'),
    ('plans-list',          'en', 'Plan List'),
    ('plans-academic',      'tr', 'Akademik Dönemler'),
    ('plans-academic',      'en', 'Academic Periods'),
    ('plans-reports',       'tr', 'Raporlar'),
    ('plans-reports',       'en', 'Reports'),
    ('plans-new',           'tr', 'Yeni Plan'),
    ('plans-new',           'en', 'New Plan')
) as t(code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- ── Step 6: Grant all new permissions to default admin role ───────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where p.code in (
    'goal_library:read','goal_library:create','goal_library:update','goal_library:delete',
    'goal_template:read','goal_template:create','goal_template:update',
    'goal_template:delete','goal_template:translate',
    'student_goal:read','student_goal:create','student_goal:update',
    'student_goal:delete','student_goal:change_status',
    'goal_progress:read','goal_progress:record',
    'academic_period:read','academic_period:manage',
    'education_plan:read','education_plan:create','education_plan:update',
    'education_plan:delete','education_plan:submit','education_plan:approve',
    'education_plan:revise','education_plan:manage_goals',
    'education_plan:add_review','education_plan:guardian_view',
    'goal_report:read','goal_report:export'
)
  and r.name = 'admin'
on conflict do nothing;
