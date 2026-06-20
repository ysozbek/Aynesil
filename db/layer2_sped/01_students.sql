-- =====================================================================
-- AyNesil Platform :: Layer 2 — Students, Guardians & Clinical/Case Management
-- Reference (ref_value) categories used here:
--   student_status, enrollment_status, diagnosis_category, guardian_relationship,
--   development_area, consent_type, institution_type
-- =====================================================================

create table students.student (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  student_no        text,                                       -- human-friendly per-tenant code
  first_name        text not null,
  last_name         text not null,
  national_id       text,                                       -- KVKK special handling (consider encryption)
  birth_date        date,
  gender            text,
  primary_campus_id uuid references core.campus(id),
  status_id         uuid references ref.ref_value(id),          -- ref_type 'student_status'
  lead_id           uuid,                                       -- origin lead (crm.lead) once converted
  photo_file_id     uuid references core.file_object(id),
  notes             text,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, student_no)
);
comment on table students.student is 'Student master record. Lifecycle status driven by configurable ref_value (student_status).';
create index ix_student_name on students.student using gin ((first_name || ' ' || last_name) gin_trgm_ops);

create table students.student_status_history (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  status_id      uuid not null references ref.ref_value(id),
  reason         text,
  changed_at     timestamptz not null default now(),
  changed_by     uuid
);

-- Multi-campus: a student may receive services at several campuses.
create table students.student_campus (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  campus_id      uuid not null references core.campus(id),
  is_primary     boolean not null default false,
  active_from    date not null default current_date,
  active_to      date,
  unique (student_id, campus_id)
);

-- ---------------------------------------------------------------------
-- Guardians (may have portal access) & emergency contacts
-- ---------------------------------------------------------------------
create table students.guardian (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  user_id        uuid references iam.user_account(id),          -- set when portal access granted
  first_name     text not null,
  last_name      text not null,
  national_id    text,
  email          citext,
  phone          text,
  occupation     text,
  address_line   text,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table students.student_guardian (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  student_id      uuid not null references students.student(id) on delete cascade,
  guardian_id     uuid not null references students.guardian(id) on delete cascade,
  relationship_id uuid references ref.ref_value(id),            -- ref_type 'guardian_relationship'
  is_primary      boolean not null default false,
  has_custody     boolean not null default true,
  portal_access   boolean not null default false,
  financial_responsible boolean not null default false,
  unique (student_id, guardian_id)
);
comment on table students.student_guardian is 'M:N student<->guardian with custody, portal, and financial-responsibility flags.';

create table students.emergency_contact (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  full_name      text not null,
  relationship   text,
  phone          text not null,
  priority       integer not null default 1
);

-- ---------------------------------------------------------------------
-- Developmental profile & diagnoses
-- ---------------------------------------------------------------------
create table students.developmental_profile (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  student_id      uuid not null references students.student(id) on delete cascade,
  development_area_id uuid references ref.ref_value(id),         -- ref_type 'development_area'
  summary         text,
  strengths       text,
  needs           text,
  assessed_on     date,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  row_version integer not null default 1
);

create table students.diagnosis (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  category_id    uuid references ref.ref_value(id),             -- ref_type 'diagnosis_category'
  icd_code       text,
  description    text,
  diagnosed_on   date,
  diagnosed_by   text,                                          -- external clinician / institution
  source_file_id uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table students.diagnosis is 'Health/special-category data (KVKK). Audited; documents stored as sensitive files.';

-- ---------------------------------------------------------------------
-- Case management: medical / development / external-institution reports, notes
-- ---------------------------------------------------------------------
create table students.medical_report (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  title          text not null,
  report_date    date,
  issuer         text,
  summary        text,
  file_id        uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table students.development_report (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  period_label   text,
  report_date    date,
  authored_by    uuid,                                          -- educator user
  content        text,
  file_id        uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table students.external_institution_report (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  student_id       uuid not null references students.student(id) on delete cascade,
  institution_name text not null,
  institution_type_id uuid references ref.ref_value(id),        -- ref_type 'institution_type'
  report_date      date,
  summary          text,
  file_id          uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table students.case_note (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  note_type      text,                                          -- free or ref-driven
  body           text not null,
  is_confidential boolean not null default false,
  authored_by    uuid,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);
