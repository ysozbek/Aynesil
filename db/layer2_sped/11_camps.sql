-- =====================================================================
-- AyNesil Platform :: Layer 2 — Camp Management
-- Reference (ref_value) categories: camp_type
-- Camp sessions/attendance reuse the scheduling domain where applicable.
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
