-- =====================================================================
-- AyNesil Platform :: Layer 2 — Contracts & Consent
-- Reference (ref_value) categories: contract_type, consent_type
-- Versioned templates + signed instances + history. Digital-signature ready.
-- =====================================================================

create table legal.contract_template (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  code           text not null,
  contract_type_id uuid references ref.ref_value(id),           -- ref_type 'contract_type'
  version        integer not null default 1,
  is_current     boolean not null default true,
  effective_from date,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, code, version)
);

create table legal.contract_template_translation (
  contract_template_id uuid not null references legal.contract_template(id) on delete cascade,
  locale  text not null references ref.locale(code),
  title   text not null,
  body    text not null,                                        -- template body / markup
  primary key (contract_template_id, locale)
);

create table legal.consent_template (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  code           text not null,
  consent_type_id uuid references ref.ref_value(id),            -- ref_type 'consent_type' (data_processing/camera_viewing/media/...)
  version        integer not null default 1,
  is_current     boolean not null default true,
  is_mandatory   boolean not null default false,
  effective_from date,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, code, version)
);

create table legal.consent_template_translation (
  consent_template_id uuid not null references legal.consent_template(id) on delete cascade,
  locale  text not null references ref.locale(code),
  title   text not null,
  body    text not null,
  primary key (consent_template_id, locale)
);

-- Signed contract instance.
create table legal.student_contract (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  template_id    uuid references legal.contract_template(id),
  template_version integer,
  guardian_id    uuid references students.guardian(id),
  status         text not null default 'draft' check (status in ('draft','sent','signed','active','expired','terminated')),
  signed_at      timestamptz,
  signed_by_name text,
  signature_method text check (signature_method in ('wet','e_sign','click_wrap')),
  signature_ref  text,                                          -- e-signature provider reference
  signed_file_id uuid references core.file_object(id),
  starts_on      date,
  ends_on        date,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table legal.student_contract is 'Signed contract instance; immutable signed PDF stored as a sensitive file. History via versioned templates + audit.';

-- Consent grant / withdrawal (KVKK). Scope examples: data_processing, camera_viewing.
create table legal.student_consent (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  guardian_id    uuid references students.guardian(id),
  template_id    uuid references legal.consent_template(id),
  consent_type_id uuid references ref.ref_value(id),
  template_version integer,
  state          text not null default 'granted' check (state in ('granted','withdrawn','expired')),
  granted_at     timestamptz,
  withdrawn_at   timestamptz,
  valid_until    date,
  evidence_file_id uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);
comment on table legal.student_consent is 'Consent ledger (KVKK). camera_viewing consent here gates media.viewing_authorization.';
create index ix_consent_student_type on legal.student_consent(corporation_id, student_id, consent_type_id);
