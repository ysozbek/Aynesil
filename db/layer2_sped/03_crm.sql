-- =====================================================================
-- AyNesil Platform :: Layer 2 — CRM / Lead & Admissions
-- Reference (ref_value) categories: lead_source, lead_status, pipeline_stage,
--   activity_type, meeting_type
-- =====================================================================

create table crm.lead (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  campus_id        uuid references core.campus(id),
  source_id        uuid references ref.ref_value(id),           -- ref_type 'lead_source' (website/phone/social/referral/walk_in)
  status_id        uuid references ref.ref_value(id),           -- ref_type 'lead_status'
  pipeline_stage_id uuid references ref.ref_value(id),          -- ref_type 'pipeline_stage'
  child_name       text,
  child_birth_date date,
  contact_name     text not null,
  contact_phone    text,
  contact_email    citext,
  presenting_need  text,                                        -- why they reached out
  referral_detail  text,
  assigned_to      uuid references iam.user_account(id),
  score            integer,
  converted_student_id uuid references students.student(id),
  converted_at     timestamptz,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table crm.lead is 'Prospect record. On enrollment, links to students.student via converted_student_id.';
create index ix_lead_pipeline on crm.lead(corporation_id, pipeline_stage_id) where deleted_at is null;

create table crm.lead_status_history (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  lead_id        uuid not null references crm.lead(id) on delete cascade,
  status_id      uuid references ref.ref_value(id),
  pipeline_stage_id uuid references ref.ref_value(id),
  changed_at     timestamptz not null default now(),
  changed_by     uuid
);

create table crm.lead_activity (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  lead_id        uuid not null references crm.lead(id) on delete cascade,
  activity_type_id uuid references ref.ref_value(id),           -- ref_type 'activity_type' (call/email/sms/note/visit)
  subject        text,
  body           text,
  direction      text check (direction in ('inbound','outbound')),
  occurred_at    timestamptz not null default now(),
  follow_up_at   timestamptz,
  performed_by   uuid references iam.user_account(id),
  created_at     timestamptz not null default now()
);
create index ix_lead_activity_followup on crm.lead_activity(corporation_id, follow_up_at) where follow_up_at is not null;

-- Pre-enrollment interview.
create table crm.interview (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  lead_id        uuid not null references crm.lead(id) on delete cascade,
  campus_id      uuid references core.campus(id),
  scheduled_at   timestamptz,
  conducted_at   timestamptz,
  conducted_by   uuid references iam.user_account(id),
  outcome        text,
  recommendation text,
  status         text not null default 'scheduled' check (status in ('scheduled','completed','no_show','cancelled')),
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);
