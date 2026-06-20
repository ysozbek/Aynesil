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
