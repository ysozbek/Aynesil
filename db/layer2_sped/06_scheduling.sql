-- =====================================================================
-- AyNesil Platform :: Layer 2 — Scheduling
-- Rooms · Sessions · Recurrence · Attendance · Make-up · Calendar
-- Reference (ref_value) categories: room_type, session_type, attendance_reason,
--   missed_reason, activity_type
-- Conflict prevention uses btree_gist EXCLUDE constraints.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Rooms (physical / therapy / classroom / online)
-- ---------------------------------------------------------------------
create table scheduling.room (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  campus_id      uuid references core.campus(id),               -- NULL for virtual/online rooms
  room_type_id   uuid references ref.ref_value(id),             -- ref_type 'room_type'
  code           text not null,
  name           text not null,
  capacity       integer not null default 1 check (capacity >= 0),
  is_virtual     boolean not null default false,
  meeting_url    text,                                          -- online room join link
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, campus_id, code)
);

-- School-wide / campus calendar entries (holidays, closures, events).
create table scheduling.calendar_entry (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  campus_id      uuid references core.campus(id),               -- NULL = corporation-wide
  title          text not null,
  entry_type     text not null default 'holiday' check (entry_type in ('holiday','closure','event','term_break')),
  starts_at      timestamptz not null,
  ends_at        timestamptz not null,
  is_all_day     boolean not null default true,
  created_at     timestamptz not null default now(),
  constraint chk_calendar_range check (ends_at > starts_at)
);

-- ---------------------------------------------------------------------
-- Recurring schedules (RRULE-style) -> generate concrete sessions
-- ---------------------------------------------------------------------
create table scheduling.recurring_schedule (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  campus_id       uuid references core.campus(id),
  student_program_id uuid references education.student_program(id),
  session_type_id uuid references ref.ref_value(id),            -- ref_type 'session_type'
  room_id         uuid references scheduling.room(id),
  frequency       text not null check (frequency in ('weekly','biweekly','monthly')),
  interval_count  integer not null default 1,
  by_weekday      smallint[],                                   -- 0=Sun .. 6=Sat
  by_monthday     smallint[],
  start_time      time not null,
  duration_minutes integer not null check (duration_minutes > 0),
  range_start     date not null,
  range_end       date,
  max_occurrences integer,
  is_active       boolean not null default true,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);
comment on table scheduling.recurring_schedule is 'Recurrence rule. A generator job materializes scheduling.session rows from this.';

create table scheduling.recurrence_exception (
  id                    uuid primary key default core.uuid_generate_v7(),
  corporation_id        uuid not null references core.corporation(id),
  recurring_schedule_id uuid not null references scheduling.recurring_schedule(id) on delete cascade,
  exception_date        date not null,
  action                text not null check (action in ('skip','reschedule','cancel')),
  new_start_at          timestamptz,
  reason                text,
  unique (recurring_schedule_id, exception_date)
);

-- ---------------------------------------------------------------------
-- Sessions (single source for individual/group/intensive/camp/online)
-- ---------------------------------------------------------------------
create table scheduling.session (
  id                    uuid primary key default core.uuid_generate_v7(),
  corporation_id        uuid not null references core.corporation(id),
  campus_id             uuid references core.campus(id),
  session_type_id       uuid not null references ref.ref_value(id),   -- ref_type 'session_type' (configurable)
  session_type_ref_type uuid not null default ref.type_id('session_type'),
  room_id               uuid references scheduling.room(id),
  recurring_schedule_id uuid references scheduling.recurring_schedule(id),
  program_service_id    uuid references education.program_service(id),
  title                 text,
  starts_at             timestamptz not null,
  ends_at               timestamptz not null,
  time_range            tstzrange generated always as (tstzrange(starts_at, ends_at, '[)')) stored,
  status                text not null default 'scheduled'
                          check (status in ('scheduled','completed','cancelled','no_show','rescheduled')),
  is_makeup             boolean not null default false,
  cancel_reason         text,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1,
  constraint chk_session_range check (ends_at > starts_at),
  -- Composite FK pins the value to the 'session_type' category at the database level:
  constraint fk_session_type
    foreign key (session_type_ref_type, session_type_id) references ref.ref_value(ref_type_id, id)
);
comment on table scheduling.session is 'Single schedulable unit. session_type is configurable; composite FK guarantees the value is a session_type.';
create index ix_session_campus_time on scheduling.session(campus_id, starts_at);
create index ix_session_room_time on scheduling.session(room_id, starts_at);

-- Prevent double-booking a physical room (overlapping, non-cancelled sessions).
alter table scheduling.session
  add constraint excl_room_overlap
  exclude using gist (room_id with =, tstzrange(starts_at, ends_at, '[)') with &&)
  where (status <> 'cancelled' and deleted_at is null and room_id is not null);

create table scheduling.session_participant (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  session_id     uuid not null references scheduling.session(id) on delete cascade,
  student_id     uuid not null references students.student(id),
  student_program_id uuid references education.student_program(id),
  role           text not null default 'student',
  unique (session_id, student_id)
);

create table scheduling.session_educator (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  session_id     uuid not null references scheduling.session(id) on delete cascade,
  educator_id    uuid not null references educators.educator(id),
  role           text not null default 'lead' check (role in ('lead','assistant','observer','supervisor')),
  unique (session_id, educator_id)
);

-- Prevent an educator being booked in two overlapping sessions.
-- (Enforced via session_educator + a deferred check or trigger; see 99_*. We add a
--  supporting unique to make conflict detection efficient.)
create index ix_session_educator_lookup on scheduling.session_educator(educator_id);

create table scheduling.session_goal (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  session_id      uuid not null references scheduling.session(id) on delete cascade,
  student_goal_id uuid not null references education.student_goal(id),
  worked_on       boolean not null default true,
  progress_note   text,
  measured_value  numeric(10,2),
  unique (session_id, student_goal_id)
);

create table scheduling.session_note (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  session_id     uuid not null references scheduling.session(id) on delete cascade,
  authored_by    uuid references educators.educator(id),
  body           text not null,
  parent_visible boolean not null default false,                -- surfaces in parent portal when true
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

-- ---------------------------------------------------------------------
-- Attendance
-- ---------------------------------------------------------------------
create table scheduling.attendance (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  session_id     uuid not null references scheduling.session(id) on delete cascade,
  student_id     uuid not null references students.student(id),
  status         text not null check (status in ('present','absent','late','excused','left_early')),
  reason_id      uuid references ref.ref_value(id),             -- ref_type 'attendance_reason'
  minutes_attended integer,
  recorded_by    uuid,
  recorded_at    timestamptz not null default now(),
  note           text,
  unique (session_id, student_id)
);
create index ix_attendance_student on scheduling.attendance(corporation_id, student_id, recorded_at);

-- ---------------------------------------------------------------------
-- Make-up sessions
-- ---------------------------------------------------------------------
create table scheduling.makeup_request (
  id                 uuid primary key default core.uuid_generate_v7(),
  corporation_id     uuid not null references core.corporation(id),
  student_id         uuid not null references students.student(id),
  missed_session_id  uuid references scheduling.session(id),
  missed_reason_id   uuid references ref.ref_value(id),         -- ref_type 'missed_reason'
  status             text not null default 'requested'
                       check (status in ('requested','approved','scheduled','completed','rejected','expired')),
  requested_by       uuid,
  requested_at       timestamptz not null default now(),
  makeup_session_id  uuid references scheduling.session(id),    -- the scheduled make-up
  completed_at       timestamptz,
  expires_on         date,
  note               text,
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);
comment on table scheduling.makeup_request is 'Tracks a missed session through make-up scheduling and completion.';
