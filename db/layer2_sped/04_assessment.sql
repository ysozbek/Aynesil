-- =====================================================================
-- AyNesil Platform :: Layer 2 — Assessment & Evaluation
-- Reference (ref_value) categories: assessment_type, assessment_category, development_area
-- History is preserved after enrollment (records are never hard-deleted).
-- =====================================================================

create table assessment.assessment_template (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid references core.corporation(id),          -- NULL = platform-provided template
  code           text not null,
  name           text not null,
  type_id        uuid references ref.ref_value(id),             -- ref_type 'assessment_type'
  category_id    uuid references ref.ref_value(id),             -- ref_type 'assessment_category'
  scoring_model  text check (scoring_model in ('sum','average','rubric','none')),
  version        integer not null default 1,
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code, version)
);

create table assessment.assessment_template_translation (
  template_id uuid not null references assessment.assessment_template(id) on delete cascade,
  locale      text not null references ref.locale(code),
  name        text not null,
  description text,
  primary key (template_id, locale)
);

create table assessment.assessment_section (
  id           uuid primary key default core.uuid_generate_v7(),
  template_id  uuid not null references assessment.assessment_template(id) on delete cascade,
  code         text not null,
  sort_order   integer not null default 0,
  development_area_id uuid references ref.ref_value(id),
  unique (template_id, code)
);

create table assessment.assessment_item (
  id          uuid primary key default core.uuid_generate_v7(),
  section_id  uuid not null references assessment.assessment_section(id) on delete cascade,
  code        text not null,
  prompt      text not null,
  response_type text not null check (response_type in ('numeric','scale','boolean','text','choice')),
  choices     jsonb,                                            -- for choice/scale items
  weight      numeric(6,2) not null default 1,
  sort_order  integer not null default 0,
  unique (section_id, code)
);

-- An assessment performed for a lead (pre-enrollment) or an enrolled student.
create table assessment.assessment_session (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  template_id    uuid not null references assessment.assessment_template(id),
  template_version integer not null default 1,
  lead_id        uuid references crm.lead(id),
  student_id     uuid references students.student(id),
  campus_id      uuid references core.campus(id),
  assessor_id    uuid references educators.educator(id),
  scheduled_at   timestamptz,
  performed_at   timestamptz,
  status         text not null default 'planned' check (status in ('planned','in_progress','completed','cancelled')),
  total_score    numeric(10,2),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  constraint chk_subject check (lead_id is not null or student_id is not null)
);
comment on table assessment.assessment_session is 'Subject is a lead OR a student; history survives lead->student conversion.';

create table assessment.assessment_response (
  id                    uuid primary key default core.uuid_generate_v7(),
  assessment_session_id uuid not null references assessment.assessment_session(id) on delete cascade,
  item_id               uuid not null references assessment.assessment_item(id),
  numeric_value         numeric(10,2),
  text_value            text,
  choice_value          text,
  note                  text,
  unique (assessment_session_id, item_id)
);

create table assessment.assessment_report (
  id                    uuid primary key default core.uuid_generate_v7(),
  corporation_id        uuid not null references core.corporation(id),
  assessment_session_id uuid not null references assessment.assessment_session(id) on delete cascade,
  summary               text,
  findings              text,
  file_id               uuid references core.file_object(id),
  finalized_at          timestamptz,
  finalized_by          uuid,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);

-- Program recommendation produced from an assessment (feeds enrollment).
create table assessment.program_recommendation (
  id                    uuid primary key default core.uuid_generate_v7(),
  corporation_id        uuid not null references core.corporation(id),
  assessment_session_id uuid references assessment.assessment_session(id),
  lead_id               uuid references crm.lead(id),
  student_id            uuid references students.student(id),
  recommended_program_id uuid,                                  -- -> education.program (soft ref to avoid cycle)
  recommended_intensity text,
  rationale             text,
  recommended_by        uuid references educators.educator(id),
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);
