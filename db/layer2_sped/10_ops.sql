-- =====================================================================
-- AyNesil Platform :: Layer 2 — Operations: Meetings, Educator Leave, Performance
-- Reference (ref_value) categories: meeting_type, leave_type, kpi_category
-- =====================================================================

-- ---------------------------------------------------------------------
-- Meetings (internal / parent / prospect / external)
-- ---------------------------------------------------------------------
create table ops.meeting (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  campus_id      uuid references core.campus(id),
  meeting_type_id uuid references ref.ref_value(id),            -- ref_type 'meeting_type'
  title          text not null,
  scheduled_at   timestamptz,
  ends_at        timestamptz,
  location       text,
  room_id        uuid references scheduling.room(id),
  status         text not null default 'scheduled' check (status in ('scheduled','completed','cancelled')),
  organizer_id   uuid references iam.user_account(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table ops.meeting_participant (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  meeting_id     uuid not null references ops.meeting(id) on delete cascade,
  participant_type text not null check (participant_type in ('user','guardian','lead','external')),
  user_id        uuid references iam.user_account(id),
  guardian_id    uuid references students.guardian(id),
  lead_id        uuid references crm.lead(id),
  external_name  text,
  attendance     text check (attendance in ('invited','attended','absent','tentative')),
  unique nulls not distinct (meeting_id, user_id, guardian_id, lead_id, external_name)
);

create table ops.meeting_outcome (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  meeting_id     uuid not null references ops.meeting(id) on delete cascade,
  summary        text,
  decisions      text,
  created_at     timestamptz not null default now(),
  created_by     uuid
);

create table ops.meeting_follow_up (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  meeting_id     uuid not null references ops.meeting(id) on delete cascade,
  action         text not null,
  assignee_id    uuid references iam.user_account(id),
  due_date       date,
  status         text not null default 'open' check (status in ('open','in_progress','done','cancelled')),
  created_at     timestamptz not null default now()
);

-- ---------------------------------------------------------------------
-- Educator leave (configurable types, approval workflow, balances, conflicts)
-- ---------------------------------------------------------------------
create table ops.leave_request (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  educator_id    uuid not null references educators.educator(id) on delete cascade,
  leave_type_id  uuid references ref.ref_value(id),             -- ref_type 'leave_type' (configurable)
  unit           text not null default 'day' check (unit in ('day','hour')),
  starts_at      timestamptz not null,
  ends_at        timestamptz not null,
  time_range     tstzrange generated always as (tstzrange(starts_at, ends_at, '[)')) stored,
  quantity       numeric(6,2),                                  -- days or hours
  reason         text,
  status         text not null default 'pending' check (status in ('pending','approved','rejected','cancelled')),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  constraint chk_leave_range check (ends_at > starts_at)
);
-- Prevent overlapping approved/pending leave for the same educator.
alter table ops.leave_request
  add constraint excl_leave_overlap
  exclude using gist (educator_id with =, tstzrange(starts_at, ends_at, '[)') with &&)
  where (status in ('pending','approved'));

create table ops.leave_approval (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  leave_request_id uuid not null references ops.leave_request(id) on delete cascade,
  step_no        integer not null default 1,
  approver_id    uuid references iam.user_account(id),
  decision       text not null check (decision in ('approved','rejected','pending')),
  comment        text,
  decided_at     timestamptz
);

create table ops.leave_balance (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  educator_id    uuid not null references educators.educator(id) on delete cascade,
  leave_type_id  uuid references ref.ref_value(id),
  period_year    integer not null,
  entitled       numeric(7,2) not null default 0,
  used           numeric(7,2) not null default 0,
  unit           text not null default 'day' check (unit in ('day','hour')),
  unique (educator_id, leave_type_id, period_year)
);

-- ---------------------------------------------------------------------
-- Educator performance (snapshots) + parent feedback (a metric source)
-- ---------------------------------------------------------------------
create table ops.parent_feedback (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  guardian_id    uuid references students.guardian(id),
  educator_id    uuid references educators.educator(id),
  session_id     uuid references scheduling.session(id),
  rating         smallint check (rating between 1 and 5),
  comment        text,
  created_at     timestamptz not null default now()
);

create table ops.educator_performance_snapshot (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  educator_id    uuid not null references educators.educator(id) on delete cascade,
  period_start   date not null,
  period_end     date not null,
  session_count  integer,
  attendance_rate numeric(5,2),
  goal_achievement_rate numeric(5,2),
  parent_feedback_avg numeric(4,2),
  utilization_rate numeric(5,2),
  detail         jsonb not null default '{}'::jsonb,
  computed_at    timestamptz not null default now(),
  unique (educator_id, period_start, period_end)
);
comment on table ops.educator_performance_snapshot is 'Periodic rollup feeding KPI dashboards (also expressible via core.kpi_value).';
