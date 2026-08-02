-- =============================================================================
-- AyNesil Platform :: Flyway V25 — Consultancy Agreements & Follow-up Activities
-- =============================================================================
-- ADDITIVE + backward-compatible. Objects created / seeded:
--   0. DDL: consultancy.consultancy_agreement
--           consultancy.follow_up_activity
--   1. ref_type:  agreement_type
--   2. ref_value: agreement_type (4 values)
--   3. ref_value_translation: tr + en for all new values
--   4. Permissions: consultancy_agreement:*, follow_up:*
--   5. Role grants: admin (only seeded role; see 02_akran_bootstrap.sql)
--   6. Menu items: consultancy.agreements, consultancy.follow_ups sub-items
--
-- Idempotent. Owner rolüyle çalışır — RLS bypass.
-- RLS tenant_isolation and updated_at triggers applied automatically by the
-- baseline 99_triggers_rls_policies.sql (consultancy schema is already in scope).
-- =============================================================================


-- ── Step 0: DDL — new tables ──────────────────────────────────────────────────

create table if not exists consultancy.consultancy_agreement (
  id                  uuid        primary key default core.uuid_generate_v7(),
  corporation_id      uuid        not null references core.corporation(id),
  consultancy_plan_id uuid        not null references consultancy.consultancy_plan(id) on delete restrict,
  institution_id      uuid        not null references consultancy.institution(id),
  agreement_type_id   uuid        references ref.ref_value(id),  -- ref_type 'agreement_type'
  title               text        not null,
  description         text,
  start_date          date,
  end_date            date,
  signed_date         date,
  status              text        not null default 'draft'
                        check (status in ('draft','sent','signed','expired','cancelled')),
  file_id             uuid        references core.file_object(id),
  signed_by_name      text,
  created_at          timestamptz not null default now(),
  created_by          uuid,
  updated_at          timestamptz not null default now(),
  updated_by          uuid,
  deleted_at          timestamptz,
  row_version         integer     not null default 1
);

comment on table consultancy.consultancy_agreement is
  'Formal agreements (contracts, MOUs) between the corporation and an institution, linked to a consultancy plan. Signed agreements are immutable.';
comment on column consultancy.consultancy_agreement.agreement_type_id is
  'FK to ref_value(agreement_type). Examples: service_agreement, consultancy_contract, nda, collaboration_mou.';
comment on column consultancy.consultancy_agreement.status is
  'Workflow: draft → sent → signed (terminal). Also: draft|sent → cancelled. signed → expired.';

create table if not exists consultancy.follow_up_activity (
  id                    uuid        primary key default core.uuid_generate_v7(),
  corporation_id        uuid        not null references core.corporation(id),
  consultancy_plan_id   uuid        references consultancy.consultancy_plan(id) on delete cascade,
  school_visit_id       uuid        references consultancy.school_visit(id) on delete cascade,
  observation_record_id uuid        references consultancy.observation_record(id) on delete set null,
  title                 text        not null,
  description           text,
  due_date              date,
  assigned_to           uuid        references educators.educator(id),
  status                text        not null default 'pending'
                          check (status in ('pending','in_progress','completed','cancelled')),
  completed_at          timestamptz,
  completed_by          uuid,
  notes                 text,
  created_at            timestamptz not null default now(),
  created_by            uuid,
  updated_at            timestamptz not null default now(),
  updated_by            uuid,
  row_version           integer     not null default 1
);

comment on table consultancy.follow_up_activity is
  'Action items arising from school visits or observation records. At least one of consultancy_plan_id or school_visit_id must be set (enforced by application layer).';
comment on column consultancy.follow_up_activity.status is
  'Workflow: pending → in_progress → completed (terminal). Also: pending|in_progress → cancelled.';


-- ── Step 1: ref_type ─────────────────────────────────────────────────────────

insert into ref.ref_type (code, name, is_system, is_hierarchical, allows_tenant_values)
values
  ('agreement_type', 'Agreement Types', false, false, true)
on conflict (code) do nothing;


-- ── Step 2: ref_values ────────────────────────────────────────────────────────

insert into ref.ref_value (ref_type_id, code, sort_order, is_default, is_system)
select ref.type_id(v.type_code), v.code, v.sort_order, v.is_default, false
from (values
  ('agreement_type', 'service_agreement',   1, true),
  ('agreement_type', 'consultancy_contract', 2, false),
  ('agreement_type', 'nda',                 3, false),
  ('agreement_type', 'collaboration_mou',   4, false)
) as v (type_code, code, sort_order, is_default)
where ref.type_id(v.type_code) is not null
on conflict do nothing;


-- ── Step 3: translations (tr + en) ────────────────────────────────────────────

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  ('agreement_type', 'service_agreement',    'tr', 'Hizmet Sözleşmesi'),
  ('agreement_type', 'service_agreement',    'en', 'Service Agreement'),
  ('agreement_type', 'consultancy_contract', 'tr', 'Danışmanlık Sözleşmesi'),
  ('agreement_type', 'consultancy_contract', 'en', 'Consultancy Contract'),
  ('agreement_type', 'nda',                  'tr', 'Gizlilik Sözleşmesi'),
  ('agreement_type', 'nda',                  'en', 'Non-Disclosure Agreement'),
  ('agreement_type', 'collaboration_mou',    'tr', 'İşbirliği Protokolü'),
  ('agreement_type', 'collaboration_mou',    'en', 'Collaboration MOU')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code        = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 4: Permission catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action, description) values
  -- Consultancy Agreements
  ('consultancy_agreement:read',   'consultancy_agreement', 'read',   'View consultancy agreements'),
  ('consultancy_agreement:create', 'consultancy_agreement', 'create', 'Create a draft consultancy agreement'),
  ('consultancy_agreement:update', 'consultancy_agreement', 'update', 'Update a draft agreement'),
  ('consultancy_agreement:delete', 'consultancy_agreement', 'delete', 'Soft-delete a draft or sent agreement'),
  ('consultancy_agreement:send',   'consultancy_agreement', 'send',   'Send an agreement to the institution (draft → sent)'),
  ('consultancy_agreement:sign',   'consultancy_agreement', 'sign',   'Record that an agreement has been signed (sent → signed)'),
  ('consultancy_agreement:expire', 'consultancy_agreement', 'expire', 'Mark a signed agreement as expired'),
  ('consultancy_agreement:cancel', 'consultancy_agreement', 'cancel', 'Cancel a draft or sent agreement'),
  -- Follow-up Activities
  ('follow_up:read',     'follow_up', 'read',     'View follow-up activities'),
  ('follow_up:create',   'follow_up', 'create',   'Create a follow-up activity'),
  ('follow_up:update',   'follow_up', 'update',   'Update a follow-up activity'),
  ('follow_up:delete',   'follow_up', 'delete',   'Delete a follow-up activity'),
  ('follow_up:start',    'follow_up', 'start',    'Start a follow-up activity (pending → in_progress)'),
  ('follow_up:complete', 'follow_up', 'complete', 'Mark a follow-up activity as completed'),
  ('follow_up:cancel',   'follow_up', 'cancel',   'Cancel a follow-up activity')
on conflict (code) do nothing;


-- ── Step 5: Role grants ────────────────────────────────────────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.role r
cross join iam.permission p
where r.code = 'admin'
  and p.code in (
    'consultancy_agreement:read', 'consultancy_agreement:create',
    'consultancy_agreement:update', 'consultancy_agreement:delete',
    'consultancy_agreement:send', 'consultancy_agreement:sign',
    'consultancy_agreement:expire', 'consultancy_agreement:cancel',
    'follow_up:read', 'follow_up:create', 'follow_up:update', 'follow_up:delete',
    'follow_up:start', 'follow_up:complete', 'follow_up:cancel'
  )
on conflict do nothing;


-- ── Step 6: Menu sub-items (append under existing consultancy parent) ─────────

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
    ('consultancy.agreements',  '/consultancy/agreements',  'file-signature', 6, 'consultancy_agreement:read'),
    ('consultancy.follow_ups',  '/consultancy/follow-ups',  'check-circle',   7, 'follow_up:read')
) as v (code, route, icon, sort_order, perm_code)
join  iam.menu_item parent on parent.code = 'consultancy' and parent.corporation_id is null
left join iam.permission p  on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('consultancy.agreements', 'tr', 'Danışmanlık Sözleşmeleri'),
    ('consultancy.agreements', 'en', 'Consultancy Agreements'),
    ('consultancy.follow_ups', 'tr', 'Takip Aktiviteleri'),
    ('consultancy.follow_ups', 'en', 'Follow-up Activities')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
