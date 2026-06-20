-- =====================================================================
-- AyNesil Platform :: Layer 2 — Programs, Enrollment, Goals & Education Plans (BEP/IEP)
-- Reference (ref_value) categories: program_type, service_type, enrollment_status,
--   goal_category, development_area, academic_term
-- =====================================================================

-- ---------------------------------------------------------------------
-- Programs & program services (therapy / education / consultation / camp / online)
-- ---------------------------------------------------------------------
create table education.program (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  code           text not null,
  name           text not null,
  program_type_id uuid references ref.ref_value(id),           -- ref_type 'program_type' (configurable)
  description    text,
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, code)
);

create table education.program_translation (
  program_id  uuid not null references education.program(id) on delete cascade,
  locale      text not null references ref.locale(code),
  name        text not null,
  description text,
  primary key (program_id, locale)
);

create table education.program_service (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  program_id     uuid not null references education.program(id) on delete cascade,
  service_type_id uuid references ref.ref_value(id),           -- ref_type 'service_type' (therapy/education/consultation/camp/online)
  name           text not null,
  default_duration_minutes integer,
  default_sessions_per_week numeric(4,1),
  sort_order     integer not null default 0
);

-- ---------------------------------------------------------------------
-- Enrollment (overall) & program enrollment
-- ---------------------------------------------------------------------
create table education.enrollment (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  campus_id      uuid references core.campus(id),
  status_id      uuid references ref.ref_value(id),            -- ref_type 'enrollment_status'
  enrolled_on    date not null default current_date,
  ended_on       date,
  termination_reason text,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table education.student_program (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  program_id     uuid not null references education.program(id),
  enrollment_id  uuid references education.enrollment(id),
  campus_id      uuid references core.campus(id),
  start_date     date,
  end_date       date,
  status         text not null default 'active' check (status in ('active','paused','completed','cancelled')),
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);
create index ix_student_program_student on education.student_program(student_id) where deleted_at is null;

-- ---------------------------------------------------------------------
-- Goal library & templates (reusable), individual student goals, progress
-- ---------------------------------------------------------------------
create table education.goal_library (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid references core.corporation(id),          -- NULL = platform-provided library
  name           text not null,
  description    text,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);

create table education.goal_template (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid references core.corporation(id),
  library_id      uuid references education.goal_library(id),
  category_id     uuid references ref.ref_value(id),            -- ref_type 'goal_category' (configurable)
  development_area_id uuid references ref.ref_value(id),        -- ref_type 'development_area'
  code            text,
  statement       text not null,
  default_criteria text,                                        -- mastery criteria
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table education.goal_template_translation (
  goal_template_id uuid not null references education.goal_template(id) on delete cascade,
  locale           text not null references ref.locale(code),
  statement        text not null,
  default_criteria text,
  primary key (goal_template_id, locale)
);

create table education.student_goal (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  student_id       uuid not null references students.student(id) on delete cascade,
  template_id      uuid references education.goal_template(id),
  category_id      uuid references ref.ref_value(id),
  development_area_id uuid references ref.ref_value(id),
  horizon          text not null default 'short_term' check (horizon in ('long_term','short_term')),
  parent_goal_id   uuid references education.student_goal(id),  -- short-term goals can nest under long-term
  statement        text not null,
  mastery_criteria text,
  baseline         text,
  target_value     numeric(10,2),
  status           text not null default 'active' check (status in ('active','achieved','discontinued','on_hold')),
  start_date       date,
  target_date      date,
  achieved_date    date,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);
create index ix_student_goal_student on education.student_goal(student_id) where deleted_at is null;

-- Progress measurements over time (trend analysis).
create table education.goal_progress (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_goal_id uuid not null references education.student_goal(id) on delete cascade,
  session_id     uuid,                                          -- -> scheduling.session (soft ref, created later)
  measured_on    date not null default current_date,
  measured_value numeric(10,2),
  percent_complete numeric(5,2),
  trend          text check (trend in ('improving','stable','declining')),
  note           text,
  recorded_by    uuid,
  created_at     timestamptz not null default now()
);
create index ix_goal_progress_goal_time on education.goal_progress(student_goal_id, measured_on);

-- ---------------------------------------------------------------------
-- Academic periods & Education Plan (BEP/IEP) with reviews/approvals/revisions
-- ---------------------------------------------------------------------
create table education.academic_period (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  name           text not null,
  term_id        uuid references ref.ref_value(id),             -- ref_type 'academic_term'
  start_date     date not null,
  end_date       date not null,
  is_current     boolean not null default false,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);

create table education.education_plan (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  student_id        uuid not null references students.student(id) on delete cascade,
  academic_period_id uuid references education.academic_period(id),
  campus_id         uuid references core.campus(id),
  title             text not null,
  version           integer not null default 1,
  status            text not null default 'draft'
                      check (status in ('draft','in_review','approved','active','revised','closed')),
  effective_from    date,
  effective_to      date,
  prepared_by       uuid references educators.educator(id),
  approved_by       uuid references educators.educator(id),     -- coordinator
  approved_at       timestamptz,
  guardian_visible  boolean not null default false,             -- guardians may view once approved
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table education.education_plan is 'Individualized Education Plan (BEP/IEP). Guardians may view once status=approved and guardian_visible=true.';

create table education.education_plan_goal (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  education_plan_id uuid not null references education.education_plan(id) on delete cascade,
  student_goal_id  uuid not null references education.student_goal(id),
  horizon          text not null default 'short_term' check (horizon in ('long_term','short_term')),
  sort_order       integer not null default 0,
  unique (education_plan_id, student_goal_id)
);

create table education.education_plan_review (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  education_plan_id uuid not null references education.education_plan(id) on delete cascade,
  reviewed_on      date not null default current_date,
  reviewer_id      uuid references educators.educator(id),
  summary          text,
  outcome          text check (outcome in ('on_track','needs_revision','met')),
  created_at       timestamptz not null default now()
);

create table education.education_plan_approval (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  education_plan_id uuid not null references education.education_plan(id) on delete cascade,
  approver_id      uuid references educators.educator(id),
  decision         text not null check (decision in ('approved','rejected','changes_requested')),
  comment          text,
  decided_at       timestamptz not null default now()
);

create table education.education_plan_revision (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  education_plan_id uuid not null references education.education_plan(id) on delete cascade,
  from_version     integer not null,
  to_version       integer not null,
  change_summary   text,
  snapshot         jsonb,                                       -- full plan snapshot at revision time
  revised_by       uuid references educators.educator(id),
  revised_at       timestamptz not null default now()
);
