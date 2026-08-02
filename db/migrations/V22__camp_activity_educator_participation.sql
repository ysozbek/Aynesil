-- =============================================================================
-- AyNesil Platform :: Flyway V22 — Camp Activities, Educators & Participation
-- =============================================================================
-- ADDITIVE + backward-compatible. Objects created / seeded:
--   1. ref_type:  camp_activity_type
--   2. ref_value: therapy / sports / social / educational (+ camp_type 'intensive')
--   3. ref_value_translation: tr + en
--   4. camps.camp_activity
--   5. camps.camp_educator
--   6. camps.camp_activity_participation
--   7. RLS tenant_isolation + updated_at triggers
--      (no trg_audit: the camps schema is intentionally excluded from the
--       baseline audit loop — 99_triggers_rls_policies.sql — which is scoped
--       to clinical/financial/legal/scheduling data. Record-level audit fields
--       still live on every camp table via BaseEntity.)
--   8. Permissions: camp_activity:*, camp_educator:*, camp_participation:*
--   9. Menu: camps.activities
--  10. Role grants: admin (only seeded role; see 02_akran_bootstrap.sql)
--
-- Idempotent. Owner rolüyle çalışır — RLS bypass.
-- =============================================================================


-- ── Step 1: ref types ─────────────────────────────────────────────────────────

insert into ref.ref_type (code, name, is_system, is_hierarchical, allows_tenant_values) values
  ('camp_activity_type', 'Camp Activity Types', false, false, true)
on conflict (code) do nothing;


-- ── Step 2: ref values ────────────────────────────────────────────────────────

insert into ref.ref_value (ref_type_id, code, sort_order, is_default, is_system)
select ref.type_id(v.type_code), v.code, v.sort_order, v.is_default, false
from (values
  ('camp_activity_type', 'therapy',      1, true),
  ('camp_activity_type', 'sports',       2, false),
  ('camp_activity_type', 'social',       3, false),
  ('camp_activity_type', 'educational',  4, false),
  -- extend camp_type with intensive (summer/winter/weekend/day already in V2)
  ('camp_type',          'intensive',    5, false)
) as v (type_code, code, sort_order, is_default)
where ref.type_id(v.type_code) is not null
on conflict do nothing;


-- ── Step 3: translations (tr + en) ────────────────────────────────────────────

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  ('camp_activity_type', 'therapy',     'tr', 'Terapi Aktivitesi'),
  ('camp_activity_type', 'therapy',     'en', 'Therapy Activity'),
  ('camp_activity_type', 'sports',      'tr', 'Spor Aktivitesi'),
  ('camp_activity_type', 'sports',      'en', 'Sports Activity'),
  ('camp_activity_type', 'social',      'tr', 'Sosyal Aktivite'),
  ('camp_activity_type', 'social',      'en', 'Social Activity'),
  ('camp_activity_type', 'educational', 'tr', 'Eğitsel Aktivite'),
  ('camp_activity_type', 'educational', 'en', 'Educational Activity'),
  ('camp_type',          'summer',      'tr', 'Yaz Kampı'),
  ('camp_type',          'summer',      'en', 'Summer Camp'),
  ('camp_type',          'winter',      'tr', 'Kış Kampı'),
  ('camp_type',          'winter',      'en', 'Winter Camp'),
  ('camp_type',          'weekend',     'tr', 'Hafta Sonu Kampı'),
  ('camp_type',          'weekend',     'en', 'Weekend Camp'),
  ('camp_type',          'day',         'tr', 'Gündüz Kampı'),
  ('camp_type',          'day',         'en', 'Day Camp'),
  ('camp_type',          'intensive',   'tr', 'Yoğun Kamp'),
  ('camp_type',          'intensive',   'en', 'Intensive Camp')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 4: camps.camp_activity ───────────────────────────────────────────────

create table if not exists camps.camp_activity (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  camp_period_id   uuid not null references camps.camp_period(id) on delete cascade,
  activity_type_id uuid references ref.ref_value(id),             -- ref_type 'camp_activity_type'
  name             text not null,
  description      text,
  starts_at        timestamptz,
  ends_at          timestamptz,
  location         text,
  capacity         integer check (capacity is null or capacity > 0),
  -- Optional bridge to scheduling.session when the activity is also scheduled.
  session_id       uuid references scheduling.session(id) on delete set null,
  is_active        boolean not null default true,
  created_at       timestamptz not null default now(),
  created_by       uuid,
  updated_at       timestamptz not null default now(),
  updated_by       uuid,
  deleted_at       timestamptz,
  row_version      integer not null default 1,
  constraint chk_camp_activity_range
    check (ends_at is null or starts_at is null or ends_at > starts_at)
);

comment on table camps.camp_activity is
  'A scheduled or planned activity within a camp period. '
  'activity_type_id → ref_value(camp_activity_type). '
  'Optional session_id links to scheduling.session for calendar reuse.';

create index if not exists ix_camp_activity_period
  on camps.camp_activity (camp_period_id, starts_at);

alter table camps.camp_activity enable row level security;

drop policy if exists tenant_isolation on camps.camp_activity;
create policy tenant_isolation on camps.camp_activity
  using  (corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());

create or replace trigger trg_set_updated_at
  before update on camps.camp_activity
  for each row execute function core.set_updated_at();


-- ── Step 5: camps.camp_educator ───────────────────────────────────────────────
-- Educator assignment at camp, period, or activity scope.
-- camp_period_id NULL  → assigned to the whole camp
-- camp_activity_id NULL → assigned at camp/period level (not a single activity)

create table if not exists camps.camp_educator (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  camp_id          uuid not null references camps.camp(id) on delete cascade,
  camp_period_id   uuid references camps.camp_period(id) on delete cascade,
  camp_activity_id uuid references camps.camp_activity(id) on delete cascade,
  educator_id      uuid not null references educators.educator(id),
  role             text not null default 'lead'
                     check (role in ('lead', 'assistant', 'observer', 'supervisor')),
  assigned_at      timestamptz not null default now(),
  assigned_by      uuid,
  unique nulls not distinct (camp_id, camp_period_id, camp_activity_id, educator_id)
);

comment on table camps.camp_educator is
  'Educator assignment scoped to a camp, optionally narrowed to a period and/or activity. '
  'role mirrors scheduling.session_educator roles.';

create index if not exists ix_camp_educator_educator
  on camps.camp_educator (educator_id);

create index if not exists ix_camp_educator_camp
  on camps.camp_educator (camp_id);

alter table camps.camp_educator enable row level security;

drop policy if exists tenant_isolation on camps.camp_educator;
create policy tenant_isolation on camps.camp_educator
  using  (corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());


-- ── Step 6: camps.camp_activity_participation ─────────────────────────────────

create table if not exists camps.camp_activity_participation (
  id                 uuid primary key default core.uuid_generate_v7(),
  corporation_id     uuid not null references core.corporation(id),
  camp_activity_id   uuid not null references camps.camp_activity(id) on delete cascade,
  camp_enrollment_id uuid not null references camps.camp_enrollment(id) on delete cascade,
  status             text not null default 'registered'
                       check (status in ('registered', 'attended', 'absent', 'excused')),
  notes              text,
  recorded_by        uuid,
  recorded_at        timestamptz not null default now(),
  unique (camp_activity_id, camp_enrollment_id)
);

comment on table camps.camp_activity_participation is
  'Tracks an enrolled student''s participation in a specific camp activity. '
  'status workflow: registered → attended | absent | excused.';

create index if not exists ix_camp_participation_enrollment
  on camps.camp_activity_participation (camp_enrollment_id);

alter table camps.camp_activity_participation enable row level security;

drop policy if exists tenant_isolation on camps.camp_activity_participation;
create policy tenant_isolation on camps.camp_activity_participation
  using  (corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());


-- ── Step 7: Permissions ───────────────────────────────────────────────────────

insert into iam.permission (code, resource, action, description) values
  ('camp_activity:read',   'camp_activity', 'read',   'View camp activities'),
  ('camp_activity:create', 'camp_activity', 'create', 'Create a camp activity'),
  ('camp_activity:update', 'camp_activity', 'update', 'Update a camp activity'),
  ('camp_activity:delete', 'camp_activity', 'delete', 'Soft-delete a camp activity'),
  ('camp_educator:read',   'camp_educator', 'read',   'View camp educator assignments'),
  ('camp_educator:manage', 'camp_educator', 'manage', 'Assign or remove camp educators'),
  ('camp_participation:read',   'camp_participation', 'read',   'View activity participation'),
  ('camp_participation:record', 'camp_participation', 'record', 'Record activity participation')
on conflict (code) do nothing;


-- ── Step 8: Role grants ───────────────────────────────────────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.role r
cross join iam.permission p
where r.code = 'admin'
  and p.code in (
    'camp_activity:read', 'camp_activity:create', 'camp_activity:update', 'camp_activity:delete',
    'camp_educator:read', 'camp_educator:manage',
    'camp_participation:read', 'camp_participation:record'
  )
on conflict do nothing;


-- ── Step 9: Menu — Activities sub-item under camps ────────────────────────────

insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select
    null,
    parent.id,
    'camps.activities',
    '/camps/activities',
    'activity',
    5,
    p.id,
    true
from iam.menu_item parent
cross join iam.permission p
where parent.code = 'camps'
  and parent.corporation_id is null
  and p.code = 'camp_activity:read'
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
  ('camps.activities', 'tr', 'Kamp Aktiviteleri'),
  ('camps.activities', 'en', 'Camp Activities')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
