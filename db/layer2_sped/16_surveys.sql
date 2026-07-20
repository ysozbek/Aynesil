-- =====================================================================
-- AyNesil Platform :: Layer 2 — Surveys & Feedback Forms (ops schema)
-- Extends ops.parent_feedback (simple session rating) with configurable,
-- multi-question survey forms targeting guardians, educators, or students.
-- Reference (ref_value) categories: survey_type
-- =====================================================================

-- ---------------------------------------------------------------------
-- Survey Definition (form header — configurable per tenant)
-- ---------------------------------------------------------------------
create table ops.survey_definition (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  type_id         uuid references ref.ref_value(id),              -- ref_type 'survey_type'
  title           text not null,
  description     text,
  -- Who fills out this survey? Guardian = parent portal, educator = internal, student = direct.
  target          text not null default 'guardian'
                    check (target in ('guardian', 'educator', 'student')),
  is_active       boolean not null default true,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table ops.survey_definition is
  'Configurable survey/feedback form header. Multi-question. '
  'Targets: guardian (parent portal), educator (internal), student.';

-- ---------------------------------------------------------------------
-- Survey Questions
-- No corporation_id: child of survey_definition; tenant isolation flows from parent RLS.
-- Emsal: assessment.assessment_section (child tablolar corporation_id taşımaz).
-- ---------------------------------------------------------------------
create table ops.survey_question (
  id              uuid primary key default core.uuid_generate_v7(),
  survey_id       uuid not null references ops.survey_definition(id) on delete cascade,
  question_text   text not null,
  question_type   text not null default 'text'
                    check (question_type in
                           ('text', 'rating', 'yes_no', 'multiple_choice', 'scale')),
  is_required     boolean not null default false,
  sort_order      integer not null default 0,
  created_at      timestamptz not null default now(),
  created_by      uuid
);
create index ix_survey_question_survey on ops.survey_question(survey_id, sort_order);
comment on column ops.survey_question.question_type is
  'text=free text, rating=1-5 stars, yes_no=boolean, '
  'multiple_choice=pick one/many from options, scale=numeric range (survey_answer_option defines range).';

-- ---------------------------------------------------------------------
-- Answer Options (for multiple_choice / scale questions)
-- ---------------------------------------------------------------------
create table ops.survey_answer_option (
  id           uuid primary key default core.uuid_generate_v7(),
  question_id  uuid not null references ops.survey_question(id) on delete cascade,
  option_text  text not null,
  option_value text,                                               -- numeric/code value for scoring/reporting
  sort_order   integer not null default 0
);
create index ix_survey_answer_option_question on ops.survey_answer_option(question_id, sort_order);

-- ---------------------------------------------------------------------
-- Survey Response (one per form submission)
-- ---------------------------------------------------------------------
create table ops.survey_response (
  id                  uuid primary key default core.uuid_generate_v7(),
  corporation_id      uuid not null references core.corporation(id),
  survey_id           uuid not null references ops.survey_definition(id),
  respondent_user_id  uuid references iam.user_account(id),        -- who submitted
  guardian_id         uuid references students.guardian(id),        -- set for guardian-submitted
  student_id          uuid references students.student(id),         -- subject student (if applicable)
  session_id          uuid references scheduling.session(id),       -- linked session (if applicable)
  submitted_at        timestamptz,
  created_at          timestamptz not null default now()
);
create index ix_survey_response_survey   on ops.survey_response(survey_id, submitted_at desc);
create index ix_survey_response_guardian on ops.survey_response(guardian_id, survey_id)
  where guardian_id is not null;
create index ix_survey_response_student  on ops.survey_response(student_id, survey_id)
  where student_id is not null;
comment on table ops.survey_response is
  'One submitted response per survey × respondent × (optional) session. '
  'submitted_at is null while the guardian is mid-form.';

-- ---------------------------------------------------------------------
-- Per-Question Answer within a Response
-- ---------------------------------------------------------------------
create table ops.survey_question_response (
  id               uuid primary key default core.uuid_generate_v7(),
  response_id      uuid not null references ops.survey_response(id) on delete cascade,
  question_id      uuid not null references ops.survey_question(id),
  answer_text      text,                                            -- free text / yes_no value
  answer_option_id uuid references ops.survey_answer_option(id),   -- chosen option for multiple_choice
  numeric_value    numeric(6, 2),                                   -- for rating / scale answers
  unique (response_id, question_id)
);
comment on table ops.survey_question_response is
  'Individual question answer. Exactly one of answer_text / answer_option_id / numeric_value '
  'should be populated depending on question_type.';
