-- =====================================================================
-- AyNesil Platform :: Layer 1 — Platform Services
-- Settings · Files · Audit/Activity · Notifications · Reporting/KPI · Integration
-- =====================================================================

-- ---------------------------------------------------------------------
-- Settings (typed, hierarchical scope: system -> corporation -> campus -> user)
-- ---------------------------------------------------------------------
create table core.setting_definition (
  id            uuid primary key default core.uuid_generate_v7(),
  key           text not null unique,                  -- 'scheduling.session.default_duration'
  data_type     text not null check (data_type in ('string','integer','decimal','boolean','json','date')),
  default_value jsonb,
  scope_levels  text[] not null default '{corporation}', -- allowed scopes for this key
  description   text,
  created_at    timestamptz not null default now()
);

create table core.setting_value (
  id             uuid primary key default core.uuid_generate_v7(),
  setting_key    text not null references core.setting_definition(key),
  scope_level    text not null check (scope_level in ('system','corporation','campus','user')),
  corporation_id uuid references core.corporation(id),
  scope_id       uuid,                                  -- campus_id or user_id depending on scope_level
  value          jsonb not null,
  updated_at     timestamptz not null default now(),
  updated_by     uuid,
  unique nulls not distinct (setting_key, scope_level, corporation_id, scope_id)
);
comment on table core.setting_value is 'Resolved at read time most-specific-wins: user > campus > corporation > system default.';

-- ---------------------------------------------------------------------
-- File management (polymorphic attachments)
-- ---------------------------------------------------------------------
create table core.file_object (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  storage_backend  text not null default 's3' check (storage_backend in ('s3','gcs','azure','local')),
  bucket           text,
  object_key       text not null,
  original_name    text not null,
  mime_type        text,
  byte_size        bigint,
  checksum_sha256  text,
  is_sensitive     boolean not null default false,      -- clinical / KVKK special category
  virus_scan_status text not null default 'pending' check (virus_scan_status in ('pending','clean','infected','skipped')),
  uploaded_by      uuid,
  created_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table core.file_attachment (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  file_id           uuid not null references core.file_object(id) on delete cascade,
  owner_schema      text not null,                      -- polymorphic owner (e.g. 'students')
  owner_table       text not null,                      -- e.g. 'medical_report'
  owner_id          uuid not null,
  purpose           text,                               -- 'avatar','report_pdf','signed_contract'...
  created_at        timestamptz not null default now(),
  created_by        uuid
);
create index ix_file_attachment_owner on core.file_attachment(owner_schema, owner_table, owner_id);

-- ---------------------------------------------------------------------
-- Audit log (data changes) & Activity log (usage) — range-partitioned
-- ---------------------------------------------------------------------
create table core.audit_log (
  id             bigint generated always as identity,
  corporation_id uuid,
  schema_name    text not null,
  table_name     text not null,
  row_id         uuid,
  action         text not null,                          -- INSERT/UPDATE/DELETE
  actor_user_id  uuid,
  old_data       jsonb,
  new_data       jsonb,
  occurred_at    timestamptz not null default now(),
  primary key (id, occurred_at)
) partition by range (occurred_at);
create index ix_audit_corp_time on core.audit_log(corporation_id, occurred_at desc);
create index ix_audit_row on core.audit_log(schema_name, table_name, row_id);

create table core.activity_log (
  id             bigint generated always as identity,
  corporation_id uuid,
  user_id        uuid,
  activity_type  text not null,                          -- 'login','view','export','download'...
  target_schema  text,
  target_table   text,
  target_id      uuid,
  context        jsonb not null default '{}'::jsonb,
  ip_address     inet,
  occurred_at    timestamptz not null default now(),
  primary key (id, occurred_at)
) partition by range (occurred_at);
create index ix_activity_corp_time on core.activity_log(corporation_id, occurred_at desc);

-- Initial DEFAULT partitions (replace with monthly partitions via pg_partman / scheduled job).
create table core.audit_log_default    partition of core.audit_log    default;
create table core.activity_log_default partition of core.activity_log default;

-- ---------------------------------------------------------------------
-- Notifications (templates, channels via ref data, deliveries, preferences)
-- ---------------------------------------------------------------------
create table core.notification_template (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid references core.corporation(id),         -- NULL = platform default template
  code             text not null,
  category_id      uuid references ref.ref_value(id),            -- ref_type 'notification_category'
  type_id          uuid references ref.ref_value(id),            -- ref_type 'notification_type'
  is_active        boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code)
);

create table core.notification_template_translation (
  template_id uuid not null references core.notification_template(id) on delete cascade,
  locale      text not null references ref.locale(code),
  subject     text,
  body        text not null,
  primary key (template_id, locale)
);

create table core.notification (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  template_id    uuid references core.notification_template(id),
  category_id    uuid references ref.ref_value(id),
  recipient_user_id uuid references iam.user_account(id),
  subject        text,
  body           text,
  payload        jsonb not null default '{}'::jsonb,
  status         text not null default 'pending' check (status in ('pending','sent','read','failed','cancelled')),
  created_at     timestamptz not null default now(),
  read_at        timestamptz
);
create index ix_notification_recipient on core.notification(recipient_user_id, status);

create table core.notification_delivery (
  id              uuid primary key default core.uuid_generate_v7(),
  notification_id uuid not null references core.notification(id) on delete cascade,
  channel_id      uuid references ref.ref_value(id),            -- ref_type 'notification_channel' (email/sms/push/in_app)
  provider_id     uuid,                                         -- -> core.integration_connection
  status          text not null default 'queued' check (status in ('queued','sent','delivered','failed','bounced')),
  attempts        integer not null default 0,
  error_detail    text,
  dispatched_at   timestamptz,
  delivered_at    timestamptz
);

create table core.notification_preference (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  user_id        uuid not null references iam.user_account(id) on delete cascade,
  category_id    uuid references ref.ref_value(id),
  channel_id     uuid references ref.ref_value(id),
  is_enabled     boolean not null default true,
  unique nulls not distinct (user_id, category_id, channel_id)
);

-- ---------------------------------------------------------------------
-- Reporting & KPI infrastructure
-- ---------------------------------------------------------------------
create table core.report_definition (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid references core.corporation(id),          -- NULL = platform-provided report
  code           text not null,
  name           text not null,
  category_id    uuid references ref.ref_value(id),             -- ref_type 'report_category'
  spec           jsonb not null default '{}'::jsonb,            -- query / dataset / column definition
  params_schema  jsonb,                                         -- JSON-Schema for parameters
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code)
);

create table core.report_schedule (
  id                   uuid primary key default core.uuid_generate_v7(),
  corporation_id       uuid not null references core.corporation(id),
  report_definition_id uuid not null references core.report_definition(id),
  cron_expression      text not null,
  params               jsonb not null default '{}'::jsonb,
  recipients           jsonb not null default '[]'::jsonb,
  is_active            boolean not null default true,
  created_at           timestamptz not null default now()
);

create table core.report_run (
  id                   uuid primary key default core.uuid_generate_v7(),
  corporation_id       uuid not null references core.corporation(id),
  report_definition_id uuid not null references core.report_definition(id),
  params               jsonb not null default '{}'::jsonb,
  status               text not null default 'running' check (status in ('running','succeeded','failed')),
  result_file_id       uuid references core.file_object(id),
  started_at           timestamptz not null default now(),
  finished_at          timestamptz,
  requested_by         uuid
);

create table core.kpi_definition (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid references core.corporation(id),
  code           text not null,
  name           text not null,
  category_id    uuid references ref.ref_value(id),             -- ref_type 'kpi_category'
  unit           text,                                          -- '%','count','hours'
  spec           jsonb not null default '{}'::jsonb,            -- formula / aggregation definition
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code)
);

create table core.kpi_value (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  kpi_id         uuid not null references core.kpi_definition(id),
  subject_type   text not null,                                 -- 'educator','campus','corporation','program'
  subject_id     uuid,
  period_start   date not null,
  period_end     date not null,
  numeric_value  numeric(18,4),
  detail         jsonb not null default '{}'::jsonb,
  computed_at    timestamptz not null default now(),
  unique (kpi_id, subject_type, subject_id, period_start, period_end)
);

-- ---------------------------------------------------------------------
-- Integration infrastructure (email/sms/payment/streaming/erp/gov/idp/mobile)
-- ---------------------------------------------------------------------
create table core.integration_provider (
  id           uuid primary key default core.uuid_generate_v7(),
  code         text not null unique,                            -- 'sendgrid','twilio','iyzico','zoom'...
  kind_id      uuid references ref.ref_value(id),               -- ref_type 'integration_kind'
  display_name text not null,
  is_active    boolean not null default true
);

create table core.integration_connection (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  provider_id    uuid not null references core.integration_provider(id),
  config         jsonb not null default '{}'::jsonb,            -- non-secret config
  secret_ref     text,                                          -- REFERENCE into a secret manager (never raw secrets)
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique (corporation_id, provider_id)
);

create table core.webhook_endpoint (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  url            text not null,
  event_types    text[] not null default '{}',
  secret_ref     text,
  is_active      boolean not null default true,
  created_at     timestamptz not null default now()
);

-- Transactional outbox for reliable external dispatch.
create table core.outbox_event (
  id             bigint generated always as identity primary key,
  corporation_id uuid,
  aggregate_type text not null,
  aggregate_id   uuid,
  event_type     text not null,
  payload        jsonb not null,
  status         text not null default 'pending' check (status in ('pending','dispatched','failed')),
  attempts       integer not null default 0,
  created_at     timestamptz not null default now(),
  dispatched_at  timestamptz
);
create index ix_outbox_pending on core.outbox_event(created_at) where status = 'pending';

create table core.integration_log (
  id             bigint generated always as identity,
  corporation_id uuid,
  connection_id  uuid references core.integration_connection(id),
  direction      text not null check (direction in ('outbound','inbound')),
  request        jsonb,
  response       jsonb,
  status_code    integer,
  occurred_at    timestamptz not null default now(),
  primary key (id, occurred_at)
) partition by range (occurred_at);
create table core.integration_log_default partition of core.integration_log default;
