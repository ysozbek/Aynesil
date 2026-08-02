-- =====================================================================
-- AyNesil Platform :: Layer 2 — Camp Management
-- Reference (ref_value) categories: camp_type, camp_activity_type,
--   attendance_reason
-- Camp sessions may optionally link to scheduling.session via session_id.
-- =====================================================================

create table camps.camp (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  campus_id      uuid references core.campus(id),
  camp_type_id   uuid references ref.ref_value(id),             -- ref_type 'camp_type'
  code           text not null,
  name           text not null,
  description    text,
  location       text,
  capacity       integer,
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, code)
);

create table camps.camp_period (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  camp_id        uuid not null references camps.camp(id) on delete cascade,
  name           text not null,
  start_date     date not null,
  end_date       date not null,
  capacity       integer,
  constraint chk_camp_period_range check (end_date >= start_date)
);

create table camps.camp_enrollment (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  camp_period_id uuid not null references camps.camp_period(id) on delete cascade,
  student_id     uuid not null references students.student(id),
  student_package_id uuid references finance.student_package(id),
  status         text not null default 'enrolled' check (status in ('enrolled','waitlist','withdrawn','completed')),
  enrolled_at    timestamptz not null default now(),
  unique (camp_period_id, student_id)
);

create table camps.camp_attendance (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  camp_enrollment_id uuid not null references camps.camp_enrollment(id) on delete cascade,
  attendance_date date not null,
  status         text not null check (status in ('present','absent','late','excused')),
  reason_id      uuid references ref.ref_value(id),             -- ref_type 'attendance_reason'
  recorded_by    uuid,
  unique (camp_enrollment_id, attendance_date)
);

create table camps.camp_report (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  camp_enrollment_id uuid not null references camps.camp_enrollment(id) on delete cascade,
  summary        text,
  file_id        uuid references core.file_object(id),
  authored_by    uuid,
  created_at     timestamptz not null default now()
);

-- ---------------------------------------------------------------------
-- Camp activities (within a period)
-- ---------------------------------------------------------------------
create table camps.camp_activity (
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
  'A planned activity within a camp period. activity_type_id → camp_activity_type. '
  'Optional session_id bridges to scheduling.session.';
create index ix_camp_activity_period on camps.camp_activity (camp_period_id, starts_at);

-- ---------------------------------------------------------------------
-- Educator assignments (camp / period / activity scoped)
-- ---------------------------------------------------------------------
create table camps.camp_educator (
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
  'Educator assignment scoped to a camp, optionally narrowed to a period and/or activity.';
create index ix_camp_educator_educator on camps.camp_educator (educator_id);
create index ix_camp_educator_camp on camps.camp_educator (camp_id);

-- ---------------------------------------------------------------------
-- Activity participation (enrolled student ↔ activity)
-- ---------------------------------------------------------------------
create table camps.camp_activity_participation (
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
  'Tracks an enrolled student''s participation in a specific camp activity.';
create index ix_camp_participation_enrollment
  on camps.camp_activity_participation (camp_enrollment_id);
