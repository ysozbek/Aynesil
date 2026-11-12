-- =====================================================================
-- AyNesil Platform :: Layer 2 — School Consultancy
-- Reference (ref_value) categories: institution_type
-- =====================================================================

create table consultancy.institution (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  institution_type_id uuid references ref.ref_value(id),        -- ref_type 'institution_type'
  name           text not null,
  city           text,
  district       text,
  contact_name   text,
  contact_phone  text,
  contact_email  citext,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table consultancy.consultancy_plan (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  institution_id uuid not null references consultancy.institution(id) on delete cascade,
  name           text not null,
  period_start   date,
  period_end     date,
  scope          text,
  lead_educator_id uuid references educators.educator(id),
  consultancy_type_id uuid references ref.ref_value(id),        -- ref_type 'consultancy_type' (V24)
  status         text not null default 'active' check (status in ('draft','active','completed','cancelled')),
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);

create table consultancy.school_visit (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  consultancy_plan_id uuid references consultancy.consultancy_plan(id) on delete cascade,
  institution_id uuid not null references consultancy.institution(id),
  visit_date     date not null,
  visitor_id     uuid references educators.educator(id),
  purpose        text,
  status         text not null default 'planned' check (status in ('planned','completed','cancelled')),
  created_at     timestamptz not null default now()
);

create table consultancy.observation_record (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  school_visit_id uuid not null references consultancy.school_visit(id) on delete cascade,
  subject        text,                                          -- class/teacher/child observed
  observation    text not null,
  recommendations text,
  observation_type_id uuid references ref.ref_value(id),        -- ref_type 'observation_type' (V24)
  created_at     timestamptz not null default now(),
  created_by     uuid
);

create table consultancy.consultancy_report (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  consultancy_plan_id uuid references consultancy.consultancy_plan(id),
  school_visit_id uuid references consultancy.school_visit(id),
  title          text not null,
  summary        text,
  file_id        uuid references core.file_object(id),
  authored_by    uuid,
  created_at     timestamptz not null default now()
);

-- ---------------------------------------------------------------------
-- Agreements and follow-up activities (synced from migration V26)
-- RLS tenant_isolation and updated_at triggers come from 99_triggers_rls_policies.sql.
-- ---------------------------------------------------------------------
create table consultancy.consultancy_agreement (
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

create table consultancy.follow_up_activity (
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
