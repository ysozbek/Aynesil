-- =====================================================================
-- AyNesil Platform :: Layer 2 — Educator Management
-- Reference (ref_value) categories: educator_title, specialty, certification_type,
--   educator_relationship (hierarchy edge type)
-- =====================================================================

create table educators.educator (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  user_id           uuid references iam.user_account(id),       -- login identity
  first_name        text not null,
  last_name         text not null,
  title_id          uuid references ref.ref_value(id),          -- ref_type 'educator_title' (configurable)
  email             citext,
  phone             text,
  employment_type   text,                                       -- full_time/part_time/contractor
  hire_date         date,
  is_active         boolean not null default true,
  primary_campus_id uuid references core.campus(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table educators.educator is 'Educator master record. Title is configurable reference data (therapist, psychologist, consultant, ...).';

create table educators.educator_campus (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  educator_id    uuid not null references educators.educator(id) on delete cascade,
  campus_id      uuid not null references core.campus(id),
  is_primary     boolean not null default false,
  active_from    date not null default current_date,
  active_to      date,
  unique (educator_id, campus_id)
);

create table educators.educator_specialty (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  educator_id    uuid not null references educators.educator(id) on delete cascade,
  specialty_id   uuid not null references ref.ref_value(id),    -- ref_type 'specialty'
  unique (educator_id, specialty_id)
);

create table educators.educator_certification (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  educator_id      uuid not null references educators.educator(id) on delete cascade,
  certification_type_id uuid references ref.ref_value(id),      -- ref_type 'certification_type'
  name             text not null,
  issuer           text,
  issued_on        date,
  expires_on       date,
  file_id          uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

-- Flexible supervisory hierarchy (educator -> consultant -> coordinator, any depth).
create table educators.educator_hierarchy (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  educator_id       uuid not null references educators.educator(id) on delete cascade,
  supervisor_id     uuid not null references educators.educator(id) on delete cascade,
  relationship_id   uuid references ref.ref_value(id),          -- ref_type 'educator_relationship'
  campus_id         uuid references core.campus(id),            -- relationship may be campus-specific
  active_from       date not null default current_date,
  active_to         date,
  constraint chk_not_self check (educator_id <> supervisor_id),
  unique nulls not distinct (educator_id, supervisor_id, relationship_id, campus_id)
);
comment on table educators.educator_hierarchy is 'Edge list for a flexible, optionally campus-scoped supervisory graph.';
