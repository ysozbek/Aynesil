-- =============================================================================
-- AyNesil Platform :: Flyway V24 — School Consultancy Management
-- =============================================================================
-- ADDITIVE + backward-compatible. Objects created / seeded:
--   0. DDL: consultancy_type_id on consultancy_plan
--           observation_type_id on observation_record
--   1. ref_type:  consultancy_type, observation_type
--   2. ref_value: institution_type (3 missing values),
--                 consultancy_type (4 values), observation_type (5 values)
--   3. ref_value_translation: tr + en for all values (including existing institution_type)
--   4. Permissions: institution:*, consultancy_plan:*, school_visit:*,
--                   observation:*, consultancy_report:*
--   5. Role grants: admin (only seeded role; see 02_akran_bootstrap.sql)
--   6. Menu items: consultancy top-level + 5 sub-items (tr + en translations)
--
-- Idempotent (ON CONFLICT DO NOTHING / IF NOT EXISTS). Owner rolüyle çalışır — RLS bypass.
--
-- NOT implementable (missing tables — future migration):
--   * consultancy.consultancy_agreement
--   * consultancy.follow_up_activity
-- =============================================================================


-- ── Step 0: Additive DDL — new FK columns ────────────────────────────────────

alter table consultancy.consultancy_plan
    add column if not exists consultancy_type_id uuid
    references ref.ref_value(id);

comment on column consultancy.consultancy_plan.consultancy_type_id
    is 'FK to ref_value(consultancy_type). Examples: observation, training, assessment, follow_up.';

alter table consultancy.observation_record
    add column if not exists observation_type_id uuid
    references ref.ref_value(id);

comment on column consultancy.observation_record.observation_type_id
    is 'FK to ref_value(observation_type). Examples: classroom, individual, group, teacher, environment.';


-- ── Step 1: ref_types ────────────────────────────────────────────────────────

insert into ref.ref_type (code, name, is_system, is_hierarchical, allows_tenant_values)
values
  ('consultancy_type', 'Consultancy Types',  false, false, true),
  ('observation_type', 'Observation Types',  false, false, true)
on conflict (code) do nothing;


-- ── Step 2: ref_values ────────────────────────────────────────────────────────

insert into ref.ref_value (ref_type_id, code, sort_order, is_default, is_system)
select ref.type_id(v.type_code), v.code, v.sort_order, v.is_default, false
from (values
  -- institution_type: add 3 missing values (kindergarten/primary_school/public_school/rehabilitation_center already in V2)
  ('institution_type', 'secondary_school',       5, false),
  ('institution_type', 'high_school',            6, false),
  ('institution_type', 'special_education_center', 7, false),
  -- consultancy_type
  ('consultancy_type', 'observation',  1, true),
  ('consultancy_type', 'training',     2, false),
  ('consultancy_type', 'assessment',   3, false),
  ('consultancy_type', 'follow_up',    4, false),
  -- observation_type
  ('observation_type', 'classroom',    1, true),
  ('observation_type', 'individual',   2, false),
  ('observation_type', 'group',        3, false),
  ('observation_type', 'teacher',      4, false),
  ('observation_type', 'environment',  5, false)
) as v (type_code, code, sort_order, is_default)
where ref.type_id(v.type_code) is not null
on conflict do nothing;


-- ── Step 3: translations (tr + en) ────────────────────────────────────────────

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  -- institution_type — existing values (no translations in V2 seed)
  ('institution_type', 'kindergarten',             'tr', 'Anaokulu'),
  ('institution_type', 'kindergarten',             'en', 'Kindergarten'),
  ('institution_type', 'primary_school',           'tr', 'İlkokul'),
  ('institution_type', 'primary_school',           'en', 'Primary School'),
  ('institution_type', 'public_school',            'tr', 'Devlet Okulu'),
  ('institution_type', 'public_school',            'en', 'Public School'),
  ('institution_type', 'rehabilitation_center',    'tr', 'Rehabilitasyon Merkezi'),
  ('institution_type', 'rehabilitation_center',    'en', 'Rehabilitation Center'),
  -- institution_type — new values
  ('institution_type', 'secondary_school',         'tr', 'Ortaokul'),
  ('institution_type', 'secondary_school',         'en', 'Secondary School'),
  ('institution_type', 'high_school',              'tr', 'Lise'),
  ('institution_type', 'high_school',              'en', 'High School'),
  ('institution_type', 'special_education_center', 'tr', 'Özel Eğitim Merkezi'),
  ('institution_type', 'special_education_center', 'en', 'Special Education Center'),
  -- consultancy_type
  ('consultancy_type', 'observation',  'tr', 'Gözlem'),
  ('consultancy_type', 'observation',  'en', 'Observation'),
  ('consultancy_type', 'training',     'tr', 'Eğitim'),
  ('consultancy_type', 'training',     'en', 'Training'),
  ('consultancy_type', 'assessment',   'tr', 'Değerlendirme'),
  ('consultancy_type', 'assessment',   'en', 'Assessment'),
  ('consultancy_type', 'follow_up',    'tr', 'Takip'),
  ('consultancy_type', 'follow_up',    'en', 'Follow-up'),
  -- observation_type
  ('observation_type', 'classroom',    'tr', 'Sınıf'),
  ('observation_type', 'classroom',    'en', 'Classroom'),
  ('observation_type', 'individual',   'tr', 'Bireysel'),
  ('observation_type', 'individual',   'en', 'Individual'),
  ('observation_type', 'group',        'tr', 'Grup'),
  ('observation_type', 'group',        'en', 'Group'),
  ('observation_type', 'teacher',      'tr', 'Öğretmen'),
  ('observation_type', 'teacher',      'en', 'Teacher'),
  ('observation_type', 'environment',  'tr', 'Ortam'),
  ('observation_type', 'environment',  'en', 'Environment')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code        = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 4: Permission catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action, description) values
  -- Institution
  ('institution:read',   'institution', 'read',   'View school/institution records'),
  ('institution:create', 'institution', 'create', 'Create a new institution'),
  ('institution:update', 'institution', 'update', 'Update institution details'),
  ('institution:delete', 'institution', 'delete', 'Soft-delete an institution'),
  -- Consultancy Plan
  ('consultancy_plan:read',     'consultancy_plan', 'read',     'View consultancy plans'),
  ('consultancy_plan:create',   'consultancy_plan', 'create',   'Create a consultancy plan'),
  ('consultancy_plan:update',   'consultancy_plan', 'update',   'Update a consultancy plan'),
  ('consultancy_plan:delete',   'consultancy_plan', 'delete',   'Cancel/delete a draft consultancy plan'),
  ('consultancy_plan:activate', 'consultancy_plan', 'activate', 'Activate a draft consultancy plan'),
  ('consultancy_plan:complete', 'consultancy_plan', 'complete', 'Mark a consultancy plan as completed'),
  ('consultancy_plan:cancel',   'consultancy_plan', 'cancel',   'Cancel an active consultancy plan'),
  -- School Visit
  ('school_visit:read',     'school_visit', 'read',     'View school visit records'),
  ('school_visit:create',   'school_visit', 'create',   'Schedule a new school visit'),
  ('school_visit:update',   'school_visit', 'update',   'Update visit details (date, visitor, purpose)'),
  ('school_visit:delete',   'school_visit', 'delete',   'Delete a planned school visit'),
  ('school_visit:complete', 'school_visit', 'complete', 'Mark a school visit as completed'),
  ('school_visit:cancel',   'school_visit', 'cancel',   'Cancel a planned school visit'),
  -- Observations
  ('observation:read',   'observation', 'read',   'View observation records'),
  ('observation:create', 'observation', 'create', 'Record an observation during a visit'),
  ('observation:update', 'observation', 'update', 'Update an observation'),
  ('observation:delete', 'observation', 'delete', 'Delete an observation record'),
  -- Consultancy Reports
  ('consultancy_report:read',   'consultancy_report', 'read',   'View consultancy reports'),
  ('consultancy_report:create', 'consultancy_report', 'create', 'Author a consultancy report'),
  ('consultancy_report:delete', 'consultancy_report', 'delete', 'Delete a consultancy report')
on conflict (code) do nothing;


-- ── Step 5: Role grants ────────────────────────────────────────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.role r
cross join iam.permission p
where r.code = 'admin'
  and p.code in (
    'institution:read',     'institution:create',   'institution:update',   'institution:delete',
    'consultancy_plan:read','consultancy_plan:create','consultancy_plan:update','consultancy_plan:delete',
    'consultancy_plan:activate','consultancy_plan:complete','consultancy_plan:cancel',
    'school_visit:read',    'school_visit:create',  'school_visit:update',  'school_visit:delete',
    'school_visit:complete','school_visit:cancel',
    'observation:read',     'observation:create',   'observation:update',   'observation:delete',
    'consultancy_report:read','consultancy_report:create','consultancy_report:delete'
  )
on conflict do nothing;


-- ── Step 6: Menu items (platform default — corporation_id NULL) ────────────────

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
    ('consultancy', '/consultancy', 'briefcase', 60, 'institution:read')
) as v (code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('consultancy', 'tr', 'Okul Danışmanlık'),
    ('consultancy', 'en', 'School Consultancy')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- Sub-items under consultancy
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
    ('consultancy.institutions', '/consultancy/institutions', 'building',   1, 'institution:read'),
    ('consultancy.plans',        '/consultancy/plans',        'clipboard',  2, 'consultancy_plan:read'),
    ('consultancy.visits',       '/consultancy/visits',       'calendar',   3, 'school_visit:read'),
    ('consultancy.observations', '/consultancy/observations', 'eye',        4, 'observation:read'),
    ('consultancy.reports',      '/consultancy/reports',      'file-text',  5, 'consultancy_report:read')
) as v (code, route, icon, sort_order, perm_code)
join  iam.menu_item parent on parent.code = 'consultancy' and parent.corporation_id is null
left join iam.permission p  on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('consultancy.institutions', 'tr', 'Kurumlar'),
    ('consultancy.institutions', 'en', 'Institutions'),
    ('consultancy.plans',        'tr', 'Danışmanlık Planları'),
    ('consultancy.plans',        'en', 'Consultancy Plans'),
    ('consultancy.visits',       'tr', 'Okul Ziyaretleri'),
    ('consultancy.visits',       'en', 'School Visits'),
    ('consultancy.observations', 'tr', 'Gözlemler'),
    ('consultancy.observations', 'en', 'Observations'),
    ('consultancy.reports',      'tr', 'Danışmanlık Raporları'),
    ('consultancy.reports',      'en', 'Consultancy Reports')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
