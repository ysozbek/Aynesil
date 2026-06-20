-- =============================================================================
-- AyNesil Platform :: Flyway V1 — Baseline DDL
-- Target: PostgreSQL 17+
-- Auto-generated from db/ source files — bağımlılık sırasına göre birleştirildi.
-- KAYNAK DOSYALARI DEĞİŞTİR, BU DOSYAYI DOĞRUDAN DÜZENLEME.
-- Kaynak sırası: db/README.md "Run order" bölümüne bakın.
-- =============================================================================


-- =============================================================================
-- Source: db/00_extensions_conventions.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: 00 — Extensions, Schemas & Shared Conventions
-- Target: PostgreSQL 17+
-- Run this file FIRST. See db/README.md for full execution order.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Extensions
-- ---------------------------------------------------------------------
create extension if not exists pgcrypto;   -- gen_random_uuid / gen_random_bytes / digest
create extension if not exists btree_gist;  -- EXCLUDE constraints (scheduling conflict prevention)
create extension if not exists pg_trgm;     -- fuzzy / partial text search
create extension if not exists unaccent;    -- accent-insensitive search (Turkish names)
create extension if not exists citext;      -- case-insensitive email / identifiers

-- ---------------------------------------------------------------------
-- Schemas (bounded contexts / module boundaries)
-- ---------------------------------------------------------------------
-- Layer 1 — generic enterprise platform
create schema if not exists core;   -- tenancy, settings, files, audit, notifications, reporting, integration
create schema if not exists iam;    -- identity, authentication, RBAC, dynamic menus
create schema if not exists ref;    -- localization + configurable reference-data engine

-- Layer 2 — special education & therapy management
create schema if not exists crm;          -- leads, admissions funnel, interviews
create schema if not exists students;     -- student master data, guardians, clinical/case management
create schema if not exists assessment;   -- assessment templates, sessions, scoring, recommendations
create schema if not exists educators;    -- educator master data, specialties, certifications, hierarchy
create schema if not exists education;    -- programs, goals, education plans (BEP/IEP)
create schema if not exists scheduling;   -- sessions, rooms, recurrence, attendance, make-up, calendar
create schema if not exists finance;      -- packages, credits, payments, invoicing, discounts
create schema if not exists legal;        -- contracts & consent (versioned templates + signed instances)
create schema if not exists media;        -- cameras & live viewing
create schema if not exists ops;          -- meetings, educator leave, performance/KPI snapshots
create schema if not exists camps;        -- camp programs
create schema if not exists consultancy;  -- school consultancy

comment on schema core is 'Layer 1: tenancy + platform services (settings, files, audit, notifications, reporting, integration).';
comment on schema iam  is 'Layer 1: identity, authentication, RBAC, dynamic menus.';
comment on schema ref  is 'Layer 1: localization + configurable reference-data engine (no schema change to add business lists).';

-- ---------------------------------------------------------------------
-- UUID v7 generator (time-ordered). On PostgreSQL 18+, replace calls
-- with the native uuidv7().
-- ---------------------------------------------------------------------
create or replace function core.uuid_generate_v7()
returns uuid
language plpgsql
volatile
as $$
begin
  return encode(
    set_bit(
      set_bit(
        overlay(
          uuid_send(gen_random_uuid())
          placing substring(int8send(floor(extract(epoch from clock_timestamp()) * 1000)::bigint) from 3)
          from 1 for 6
        ),
        52, 1),
      53, 1),
    'hex')::uuid;
end;
$$;
comment on function core.uuid_generate_v7() is 'Time-ordered UUIDv7 (timestamp in high 48 bits). Replace with native uuidv7() on PG18+.';

-- ---------------------------------------------------------------------
-- Tenant & actor context (set per request by the application layer)
--   set_config('app.current_corporation_id', '<uuid>', false);
--   set_config('app.current_user_id',        '<uuid>', false);
-- ---------------------------------------------------------------------
create or replace function core.current_corporation_id()
returns uuid
language sql
stable
as $$
  select nullif(current_setting('app.current_corporation_id', true), '')::uuid;
$$;
comment on function core.current_corporation_id() is 'Tenant context from session GUC; NULL when unset => RLS default-deny.';

create or replace function core.current_user_id()
returns uuid
language sql
stable
as $$
  select nullif(current_setting('app.current_user_id', true), '')::uuid;
$$;

-- ---------------------------------------------------------------------
-- updated_at + optimistic-lock maintenance
-- ---------------------------------------------------------------------
create or replace function core.set_updated_at()
returns trigger
language plpgsql
as $$
begin
  new.updated_at := now();
  -- Bump optimistic-lock version only when the column exists and was not changed by the caller.
  if tg_op = 'UPDATE' and (to_jsonb(new) ? 'row_version') and new.row_version = old.row_version then
    new.row_version := old.row_version + 1;
  end if;
  return new;
end;
$$;

-- ---------------------------------------------------------------------
-- Generic audit trigger -> core.audit_log (before/after JSONB capture).
-- Attached selectively to clinical/financial/legal tables in 99_*.
-- ---------------------------------------------------------------------
create or replace function core.audit_trigger()
returns trigger
language plpgsql
as $$
declare
  v_old jsonb := case when tg_op in ('UPDATE','DELETE') then to_jsonb(old) end;
  v_new jsonb := case when tg_op in ('INSERT','UPDATE') then to_jsonb(new) end;
begin
  insert into core.audit_log(
    corporation_id, schema_name, table_name, row_id, action,
    actor_user_id, old_data, new_data, occurred_at)
  values (
    coalesce((v_new->>'corporation_id')::uuid, (v_old->>'corporation_id')::uuid, core.current_corporation_id()),
    tg_table_schema, tg_table_name,
    coalesce((v_new->>'id')::uuid, (v_old->>'id')::uuid),
    tg_op, core.current_user_id(), v_old, v_new, now());
  return null; -- AFTER trigger
end;
$$;


-- =============================================================================
-- Source: db/layer1_core/01_localization.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 1 — Localization
-- =====================================================================

create table ref.locale (
  code         text primary key,                       -- BCP-47: 'tr', 'en', 'en-US'
  english_name text not null,
  native_name  text not null,
  direction    text not null default 'ltr' check (direction in ('ltr','rtl')),
  is_active    boolean not null default true,
  sort_order   integer not null default 0,
  created_at   timestamptz not null default now()
);
comment on table ref.locale is 'Supported platform locales (system reference data).';

-- Static UI / system string translations (admin-managed key/value catalog).
create table ref.i18n_message (
  id         uuid primary key default core.uuid_generate_v7(),
  namespace  text not null,            -- 'ui.menu', 'validation', 'email.subject'...
  msg_key    text not null,
  locale     text not null references ref.locale(code),
  value      text not null,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  row_version integer not null default 1,
  unique (namespace, msg_key, locale)
);
comment on table ref.i18n_message is 'Static UI/system string translations. Entity content is translated via per-entity *_translation tables.';

-- Localized label resolver for reference values (fallback chain handled in 03).
-- (Function created in 03_reference_data.sql after ref_value/translation exist.)


-- =============================================================================
-- Source: db/layer1_core/02_tenancy.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 1 — Tenancy & Organization
-- corporation = tenant root; campus = branch (sub-scope, not isolation boundary)
-- =====================================================================

create table core.corporation (
  id               uuid primary key default core.uuid_generate_v7(),
  code             text not null unique,                       -- machine slug, e.g. 'akran'
  legal_name       text not null,
  display_name     text not null,
  default_locale   text not null default 'tr' references ref.locale(code),
  default_currency char(3) not null default 'TRY',
  timezone         text not null default 'Europe/Istanbul',
  tax_office       text,
  tax_number       text,
  status           text not null default 'active' check (status in ('active','suspended','closed')),
  settings         jsonb not null default '{}'::jsonb,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table core.corporation is 'Tenant root. Every tenant-scoped row carries corporation_id and is isolated by RLS.';

create table core.campus (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  code           text not null,
  name           text not null,
  address_line   text,
  city           text,
  district       text,
  phone          text,
  email          citext,
  timezone       text,                                   -- overrides corporation timezone when set
  geo_lat        numeric(9,6),
  geo_lng        numeric(9,6),
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, code)
);
comment on table core.campus is 'Branch / campus under a corporation. Cross-campus access is governed by RBAC scope, not RLS.';

create index ix_campus_corp on core.campus(corporation_id) where deleted_at is null;


-- =============================================================================
-- Source: db/layer1_core/03_reference_data.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 1 — Configurable Reference-Data Engine
-- ---------------------------------------------------------------------
-- One generic structure serves ALL business lists (session types, therapy
-- types, lead sources, diagnosis categories, ...). Adding a new business
-- list = INSERT into ref.ref_type. No schema change ever required.
--
-- Three scopes, one structure:
--   * System            : is_system=true,  corporation_id IS NULL  (engine depends on it)
--   * Configurable/Global: shipped defaults, corporation_id IS NULL (tenants may override/extend)
--   * Tenant-specific   : corporation_id = <tenant>                 (visible to that tenant only)
-- =====================================================================

-- Catalog of reference-data CATEGORIES. New business list = one row here.
create table ref.ref_type (
  id                   uuid primary key default core.uuid_generate_v7(),
  code                 text not null unique,                  -- 'session_type','therapy_type',...
  name                 text not null,
  description          text,
  is_system            boolean not null default false,        -- platform engine depends on it
  is_hierarchical      boolean not null default false,        -- parent_value_id meaningful?
  allows_tenant_values boolean not null default true,         -- can tenants add their own values?
  value_schema         jsonb,                                 -- optional JSON-Schema validating ref_value.metadata
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);
comment on table ref.ref_type is 'Catalog of reference-data categories. Adding a new business list is data, not DDL.';

-- The actual VALUES within each category.
create table ref.ref_value (
  id              uuid not null default core.uuid_generate_v7(),
  ref_type_id     uuid not null references ref.ref_type(id),
  corporation_id  uuid references core.corporation(id),        -- NULL = system/global; set = tenant-specific
  code            text not null,
  parent_value_id uuid references ref.ref_value(id),           -- hierarchy (category -> subcategory, etc.)
  sort_order      integer not null default 0,
  is_active       boolean not null default true,
  is_default      boolean not null default false,
  is_system       boolean not null default false,              -- platform-owned; tenants cannot delete
  metadata        jsonb not null default '{}'::jsonb,          -- type-specific attrs (color, default_duration, billable...)
  effective_from  date,
  effective_to    date,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1,
  primary key (id),
  -- enables composite FK pinning so a business column can only point at the RIGHT category:
  unique (ref_type_id, id),
  -- one value code per (category, scope). NULLS NOT DISTINCT => only one system row per code.
  constraint uq_ref_value_code unique nulls not distinct (ref_type_id, corporation_id, code)
);
comment on table ref.ref_value is 'Reference values for a category. corporation_id NULL = system/global; set = tenant-specific.';
comment on column ref.ref_value.metadata is 'Type-specific attributes validated against ref_type.value_schema (e.g. session_type.default_duration_minutes).';

create index ix_ref_value_lookup on ref.ref_value(ref_type_id, corporation_id, is_active, sort_order) where deleted_at is null;
create index ix_ref_value_parent on ref.ref_value(parent_value_id) where parent_value_id is not null;

-- Exactly one default value per category per scope (system scope keyed by all-zero sentinel).
create unique index uq_ref_value_one_default
  on ref.ref_value (ref_type_id, coalesce(corporation_id, '00000000-0000-0000-0000-000000000000'::uuid))
  where is_default and deleted_at is null;

-- Translations of reference values (per-entity translation table => referential integrity).
create table ref.ref_value_translation (
  ref_value_id uuid not null references ref.ref_value(id) on delete cascade,
  locale       text not null references ref.locale(code),
  label        text not null,
  short_label  text,
  description  text,
  primary key (ref_value_id, locale)
);
comment on table ref.ref_value_translation is 'Localized labels for reference values. Fallback resolved by ref.value_label().';

-- Per-tenant overlay: deactivate / reorder / re-default a SYSTEM or GLOBAL value
-- WITHOUT mutating the shared row.
create table ref.ref_value_tenant_override (
  corporation_id uuid not null references core.corporation(id),
  ref_value_id   uuid not null references ref.ref_value(id),
  is_active      boolean,
  is_default     boolean,
  sort_order     integer,
  updated_at     timestamptz not null default now(),
  updated_by     uuid,
  primary key (corporation_id, ref_value_id)
);
comment on table ref.ref_value_tenant_override is 'Tenant preferences over shared (system/global) reference values; keeps shared rows immutable.';

-- ---------------------------------------------------------------------
-- Helpers
-- ---------------------------------------------------------------------
-- Resolve a ref_type id by stable code (used to pin composite FKs).
create or replace function ref.type_id(p_code text)
returns uuid language sql stable as $$
  select id from ref.ref_type where code = p_code;
$$;

-- Localized label with fallback: requested -> corporation default -> platform 'en' -> code.
create or replace function ref.value_label(p_ref_value_id uuid, p_locale text default null)
returns text
language sql
stable
as $$
  with want as (
    select coalesce(
      p_locale,
      (select default_locale from core.corporation where id = core.current_corporation_id()),
      'tr'
    ) as loc
  )
  select coalesce(
    (select t.label from ref.ref_value_translation t, want where t.ref_value_id = p_ref_value_id and t.locale = want.loc),
    (select t.label from ref.ref_value_translation t where t.ref_value_id = p_ref_value_id and t.locale = 'en'),
    (select v.code  from ref.ref_value v where v.id = p_ref_value_id)
  );
$$;
comment on function ref.value_label(uuid, text) is 'Localized reference label with fallback chain: requested -> corporation default -> en -> code.';

-- Effective view that merges shared values with tenant overrides for the current tenant.
create or replace view ref.v_effective_ref_value as
select
  v.id,
  v.ref_type_id,
  rt.code                                   as type_code,
  v.corporation_id,
  v.code,
  v.parent_value_id,
  coalesce(o.sort_order, v.sort_order)      as sort_order,
  coalesce(o.is_active,  v.is_active)       as is_active,
  coalesce(o.is_default, v.is_default)      as is_default,
  v.is_system,
  v.metadata,
  ref.value_label(v.id)                     as label
from ref.ref_value v
join ref.ref_type rt on rt.id = v.ref_type_id
left join ref.ref_value_tenant_override o
       on o.ref_value_id = v.id
      and o.corporation_id = core.current_corporation_id()
where v.deleted_at is null
  and (v.corporation_id is null or v.corporation_id = core.current_corporation_id());
comment on view ref.v_effective_ref_value is 'Tenant-effective reference values: shared + tenant rows, with tenant overrides applied.';


-- =============================================================================
-- Source: db/layer1_core/04_identity_access.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 1 — Identity, Authentication, RBAC, Menus
-- =====================================================================

-- ---------------------------------------------------------------------
-- Users & external identity providers (SSO/OIDC/SAML modeled from day 1)
-- ---------------------------------------------------------------------
create table iam.user_account (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  username          citext not null,
  email             citext,
  phone             text,
  full_name         text not null,
  password_hash     text,                                   -- NULL when authenticated only via external IdP
  status            text not null default 'active' check (status in ('invited','active','suspended','disabled')),
  preferred_locale  text references ref.locale(code),
  primary_campus_id uuid references core.campus(id),
  mfa_enabled       boolean not null default false,
  mfa_secret        text,
  last_login_at     timestamptz,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, username)
);
comment on table iam.user_account is 'Authentication identity. Educators and guardians link to a user_account for portal/app access.';
create unique index uq_user_email on iam.user_account(corporation_id, email) where email is not null and deleted_at is null;

create table iam.identity_provider (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid references core.corporation(id),       -- NULL = platform-wide provider
  code           text not null,
  kind           text not null check (kind in ('oidc','saml','oauth2','ldap','local')),
  display_name   text not null,
  config         jsonb not null default '{}'::jsonb,         -- endpoints, client id, secret REFERENCE (not raw secret)
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code)
);

create table iam.user_identity (
  id               uuid primary key default core.uuid_generate_v7(),
  user_id          uuid not null references iam.user_account(id) on delete cascade,
  provider_id      uuid not null references iam.identity_provider(id),
  external_subject text not null,
  created_at       timestamptz not null default now(),
  unique (provider_id, external_subject)
);

create table iam.auth_session (
  id                 uuid primary key default core.uuid_generate_v7(),
  corporation_id     uuid not null references core.corporation(id),
  user_id            uuid not null references iam.user_account(id) on delete cascade,
  issued_at          timestamptz not null default now(),
  expires_at         timestamptz not null,
  revoked_at         timestamptz,
  refresh_token_hash text,
  ip_address         inet,
  user_agent         text
);
create index ix_auth_session_user on iam.auth_session(user_id) where revoked_at is null;

-- ---------------------------------------------------------------------
-- RBAC: roles, permissions, assignments (campus-scopable)
-- ---------------------------------------------------------------------
create table iam.permission (
  id          uuid primary key default core.uuid_generate_v7(),
  code        text not null unique,           -- 'student:read', 'session:create'
  resource    text not null,
  action      text not null,
  description text
);
comment on table iam.permission is 'Platform catalog of resource:action permissions (global, not tenant-scoped).';

create table iam.role (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid references core.corporation(id),       -- NULL = system role template (cloned per tenant)
  code           text not null,
  name           text not null,
  description    text,
  is_system      boolean not null default false,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code)
);

create table iam.role_permission (
  role_id       uuid not null references iam.role(id) on delete cascade,
  permission_id uuid not null references iam.permission(id) on delete cascade,
  primary key (role_id, permission_id)
);

create table iam.user_role (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  user_id        uuid not null references iam.user_account(id) on delete cascade,
  role_id        uuid not null references iam.role(id),
  campus_id      uuid references core.campus(id),             -- NULL = corporation-wide; set = campus-scoped grant
  valid_from     timestamptz,
  valid_to       timestamptz,
  created_at     timestamptz not null default now(),
  created_by     uuid,
  unique nulls not distinct (user_id, role_id, campus_id)
);
comment on table iam.user_role is 'Role grant, optionally scoped to a single campus (scoped authorization).';

-- ---------------------------------------------------------------------
-- Dynamic, permission-driven menus
-- ---------------------------------------------------------------------
create table iam.menu_item (
  id                     uuid primary key default core.uuid_generate_v7(),
  corporation_id         uuid references core.corporation(id),     -- NULL = platform default menu
  parent_id              uuid references iam.menu_item(id),
  code                   text not null,
  route                  text,
  icon                   text,
  sort_order             integer not null default 0,
  required_permission_id uuid references iam.permission(id),       -- visibility driven by permission
  feature_flag           text,
  is_active              boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code)
);

create table iam.menu_item_translation (
  menu_item_id uuid not null references iam.menu_item(id) on delete cascade,
  locale       text not null references ref.locale(code),
  label        text not null,
  primary key (menu_item_id, locale)
);


-- =============================================================================
-- Source: db/layer1_core/05_platform_services.sql
-- =============================================================================

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


-- =============================================================================
-- Source: db/layer2_sped/01_students.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 2 — Students, Guardians & Clinical/Case Management
-- Reference (ref_value) categories used here:
--   student_status, enrollment_status, diagnosis_category, guardian_relationship,
--   development_area, consent_type, institution_type
-- =====================================================================

create table students.student (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  student_no        text,                                       -- human-friendly per-tenant code
  first_name        text not null,
  last_name         text not null,
  national_id       text,                                       -- KVKK special handling (consider encryption)
  birth_date        date,
  gender            text,
  primary_campus_id uuid references core.campus(id),
  status_id         uuid references ref.ref_value(id),          -- ref_type 'student_status'
  lead_id           uuid,                                       -- origin lead (crm.lead) once converted
  photo_file_id     uuid references core.file_object(id),
  notes             text,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, student_no)
);
comment on table students.student is 'Student master record. Lifecycle status driven by configurable ref_value (student_status).';
create index ix_student_name on students.student using gin ((first_name || ' ' || last_name) gin_trgm_ops);

create table students.student_status_history (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  status_id      uuid not null references ref.ref_value(id),
  reason         text,
  changed_at     timestamptz not null default now(),
  changed_by     uuid
);

-- Multi-campus: a student may receive services at several campuses.
create table students.student_campus (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  campus_id      uuid not null references core.campus(id),
  is_primary     boolean not null default false,
  active_from    date not null default current_date,
  active_to      date,
  unique (student_id, campus_id)
);

-- ---------------------------------------------------------------------
-- Guardians (may have portal access) & emergency contacts
-- ---------------------------------------------------------------------
create table students.guardian (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  user_id        uuid references iam.user_account(id),          -- set when portal access granted
  first_name     text not null,
  last_name      text not null,
  national_id    text,
  email          citext,
  phone          text,
  occupation     text,
  address_line   text,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table students.student_guardian (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  student_id      uuid not null references students.student(id) on delete cascade,
  guardian_id     uuid not null references students.guardian(id) on delete cascade,
  relationship_id uuid references ref.ref_value(id),            -- ref_type 'guardian_relationship'
  is_primary      boolean not null default false,
  has_custody     boolean not null default true,
  portal_access   boolean not null default false,
  financial_responsible boolean not null default false,
  unique (student_id, guardian_id)
);
comment on table students.student_guardian is 'M:N student<->guardian with custody, portal, and financial-responsibility flags.';

create table students.emergency_contact (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  full_name      text not null,
  relationship   text,
  phone          text not null,
  priority       integer not null default 1
);

-- ---------------------------------------------------------------------
-- Developmental profile & diagnoses
-- ---------------------------------------------------------------------
create table students.developmental_profile (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  student_id      uuid not null references students.student(id) on delete cascade,
  development_area_id uuid references ref.ref_value(id),         -- ref_type 'development_area'
  summary         text,
  strengths       text,
  needs           text,
  assessed_on     date,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  row_version integer not null default 1
);

create table students.diagnosis (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  category_id    uuid references ref.ref_value(id),             -- ref_type 'diagnosis_category'
  icd_code       text,
  description    text,
  diagnosed_on   date,
  diagnosed_by   text,                                          -- external clinician / institution
  source_file_id uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table students.diagnosis is 'Health/special-category data (KVKK). Audited; documents stored as sensitive files.';

-- ---------------------------------------------------------------------
-- Case management: medical / development / external-institution reports, notes
-- ---------------------------------------------------------------------
create table students.medical_report (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  title          text not null,
  report_date    date,
  issuer         text,
  summary        text,
  file_id        uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table students.development_report (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  period_label   text,
  report_date    date,
  authored_by    uuid,                                          -- educator user
  content        text,
  file_id        uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table students.external_institution_report (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  student_id       uuid not null references students.student(id) on delete cascade,
  institution_name text not null,
  institution_type_id uuid references ref.ref_value(id),        -- ref_type 'institution_type'
  report_date      date,
  summary          text,
  file_id          uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table students.case_note (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  note_type      text,                                          -- free or ref-driven
  body           text not null,
  is_confidential boolean not null default false,
  authored_by    uuid,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);


-- =============================================================================
-- Source: db/layer2_sped/02_educators.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 2 — Educator Management
-- Reference (ref_value) categories: educator_title, specialty, certification_type,
--   educator_relationship (hierarchy edge type)
-- =====================================================================

create table educators.educator (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  user_id           uuid references iam.user_account(id),       -- login identity
  first_name        text not null,
  last_name         text not null,
  title_id          uuid references ref.ref_value(id),          -- ref_type 'educator_title' (configurable)
  email             citext,
  phone             text,
  employment_type   text,                                       -- full_time/part_time/contractor
  hire_date         date,
  is_active         boolean not null default true,
  primary_campus_id uuid references core.campus(id),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table educators.educator is 'Educator master record. Title is configurable reference data (therapist, psychologist, consultant, ...).';

create table educators.educator_campus (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  educator_id    uuid not null references educators.educator(id) on delete cascade,
  campus_id      uuid not null references core.campus(id),
  is_primary     boolean not null default false,
  active_from    date not null default current_date,
  active_to      date,
  unique (educator_id, campus_id)
);

create table educators.educator_specialty (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  educator_id    uuid not null references educators.educator(id) on delete cascade,
  specialty_id   uuid not null references ref.ref_value(id),    -- ref_type 'specialty'
  unique (educator_id, specialty_id)
);

create table educators.educator_certification (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  educator_id      uuid not null references educators.educator(id) on delete cascade,
  certification_type_id uuid references ref.ref_value(id),      -- ref_type 'certification_type'
  name             text not null,
  issuer           text,
  issued_on        date,
  expires_on       date,
  file_id          uuid references core.file_object(id),
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

-- Flexible supervisory hierarchy (educator -> consultant -> coordinator, any depth).
create table educators.educator_hierarchy (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  educator_id       uuid not null references educators.educator(id) on delete cascade,
  supervisor_id     uuid not null references educators.educator(id) on delete cascade,
  relationship_id   uuid references ref.ref_value(id),          -- ref_type 'educator_relationship'
  campus_id         uuid references core.campus(id),            -- relationship may be campus-specific
  active_from       date not null default current_date,
  active_to         date,
  constraint chk_not_self check (educator_id <> supervisor_id),
  unique nulls not distinct (educator_id, supervisor_id, relationship_id, campus_id)
);
comment on table educators.educator_hierarchy is 'Edge list for a flexible, optionally campus-scoped supervisory graph.';


-- =============================================================================
-- Source: db/layer2_sped/03_crm.sql
-- =============================================================================

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


-- =============================================================================
-- Source: db/layer2_sped/04_assessment.sql
-- =============================================================================

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


-- =============================================================================
-- Source: db/layer2_sped/05_education.sql
-- =============================================================================

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


-- =============================================================================
-- Source: db/layer2_sped/06_scheduling.sql
-- =============================================================================

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


-- =============================================================================
-- Source: db/layer2_sped/07_finance.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 2 — Finance (Packages, Credits, Payments, Invoicing)
-- Reference (ref_value) categories: package_type, payment_method, discount_type,
--   scholarship_type
-- Money: numeric(14,2) + currency; balances derived from an append-only ledger.
-- =====================================================================

create table finance.package_definition (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  code           text not null,
  name           text not null,
  package_type_id uuid references ref.ref_value(id),            -- ref_type 'package_type' (session_package/program_package)
  program_id     uuid references education.program(id),
  total_credits  numeric(10,2),                                 -- e.g. number of sessions
  validity_days  integer,                                       -- expiration window after purchase
  list_price     numeric(14,2) not null default 0,
  currency       char(3) not null default 'TRY',
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, code)
);

create table finance.student_package (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  package_definition_id uuid references finance.package_definition(id),
  purchased_on   date not null default current_date,
  expires_on     date,
  total_credits  numeric(10,2) not null default 0,
  price          numeric(14,2) not null default 0,
  currency       char(3) not null default 'TRY',
  status         text not null default 'active' check (status in ('active','exhausted','expired','cancelled')),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table finance.student_package is 'Purchased package instance. Remaining credits = SUM(finance.credit_ledger.delta).';

-- Append-only ledger: every credit grant/consumption/adjustment is one immutable row.
create table finance.credit_ledger (
  id                 uuid primary key default core.uuid_generate_v7(),
  corporation_id     uuid not null references core.corporation(id),
  student_package_id uuid not null references finance.student_package(id) on delete cascade,
  entry_type         text not null check (entry_type in ('grant','consume','refund','adjustment','expire')),
  delta              numeric(10,2) not null,                    -- + grant / - consume
  session_id         uuid references scheduling.session(id),    -- when consumed by a session
  reason             text,
  occurred_at        timestamptz not null default now(),
  created_by         uuid
);
create index ix_credit_ledger_pkg on finance.credit_ledger(student_package_id, occurred_at);

create table finance.invoice (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid references students.student(id),
  guardian_id    uuid references students.guardian(id),
  invoice_no     text,
  issue_date     date not null default current_date,
  due_date       date,
  currency       char(3) not null default 'TRY',
  subtotal       numeric(14,2) not null default 0,
  discount_total numeric(14,2) not null default 0,
  tax_total      numeric(14,2) not null default 0,
  grand_total    numeric(14,2) not null default 0,
  status         text not null default 'draft' check (status in ('draft','issued','paid','partial','void','overdue')),
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, invoice_no)
);

create table finance.invoice_line (
  id          uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  invoice_id  uuid not null references finance.invoice(id) on delete cascade,
  description text not null,
  student_package_id uuid references finance.student_package(id),
  quantity    numeric(10,2) not null default 1,
  unit_price  numeric(14,2) not null default 0,
  line_total  numeric(14,2) not null default 0,
  sort_order  integer not null default 0
);

create table finance.payment (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  invoice_id      uuid references finance.invoice(id),
  student_id      uuid references students.student(id),
  payment_method_id uuid references ref.ref_value(id),          -- ref_type 'payment_method'
  amount          numeric(14,2) not null,
  currency        char(3) not null default 'TRY',
  status          text not null default 'captured' check (status in ('pending','authorized','captured','failed','refunded')),
  gateway_provider_id uuid references core.integration_connection(id),
  gateway_reference text,
  idempotency_key text,                                         -- gateway callback dedupe
  paid_at         timestamptz,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, idempotency_key)
);

create table finance.refund (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  payment_id     uuid not null references finance.payment(id),
  amount         numeric(14,2) not null,
  reason         text,
  status         text not null default 'pending' check (status in ('pending','processed','failed')),
  processed_at   timestamptz,
  created_at     timestamptz not null default now()
);

create table finance.discount (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  invoice_id     uuid references finance.invoice(id),
  student_package_id uuid references finance.student_package(id),
  discount_type_id uuid references ref.ref_value(id),           -- ref_type 'discount_type'
  is_percentage  boolean not null default true,
  value          numeric(14,2) not null,
  reason         text,
  created_at     timestamptz not null default now()
);

create table finance.scholarship (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  student_id       uuid not null references students.student(id),
  scholarship_type_id uuid references ref.ref_value(id),        -- ref_type 'scholarship_type'
  percentage       numeric(5,2),
  amount           numeric(14,2),
  valid_from       date,
  valid_to         date,
  approved_by      uuid,
  note             text,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);

create table finance.promotion (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  code           text not null,
  name           text not null,
  is_percentage  boolean not null default true,
  value          numeric(14,2) not null,
  valid_from     date,
  valid_to       date,
  max_redemptions integer,
  is_active      boolean not null default true,
  unique (corporation_id, code)
);


-- =============================================================================
-- Source: db/layer2_sped/08_legal.sql
-- =============================================================================

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


-- =============================================================================
-- Source: db/layer2_sped/09_media.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 2 — Cameras & Live Viewing
-- Parent live-viewing is gated by KVKK consent (legal.student_consent) and
-- time-boxed authorizations. All viewing is access-logged.
-- =====================================================================

create table media.camera (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  campus_id      uuid references core.campus(id),
  code           text not null,
  name           text not null,
  stream_provider_id uuid references core.integration_connection(id),  -- streaming provider (vendor-agnostic)
  stream_ref     text,                                          -- provider-specific stream id (no raw secrets)
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, code)
);

create table media.room_camera (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  room_id        uuid not null references scheduling.room(id) on delete cascade,
  camera_id      uuid not null references media.camera(id) on delete cascade,
  unique (room_id, camera_id)
);

create table media.session_camera (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  session_id     uuid not null references scheduling.session(id) on delete cascade,
  camera_id      uuid not null references media.camera(id),
  unique (session_id, camera_id)
);

-- Time-boxed parent authorization to view a session/student feed.
create table media.viewing_authorization (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  guardian_id    uuid not null references students.guardian(id) on delete cascade,
  student_id     uuid not null references students.student(id),
  session_id     uuid references scheduling.session(id),        -- NULL => standing authorization for the student
  consent_id     uuid references legal.student_consent(id),     -- the camera_viewing consent backing this grant
  valid_from     timestamptz not null default now(),
  valid_to       timestamptz,
  granted_by     uuid,
  is_revoked     boolean not null default false,
  created_at     timestamptz not null default now()
);
comment on table media.viewing_authorization is 'Authorizes a guardian to view a feed; should reference an active camera_viewing consent.';
create index ix_viewing_auth_guardian on media.viewing_authorization(guardian_id) where is_revoked = false;

-- Immutable access log of who watched what, when (privacy/audit).
create table media.viewing_log (
  id             bigint generated always as identity,
  corporation_id uuid not null,
  guardian_id    uuid,
  user_id        uuid,
  session_id     uuid,
  camera_id      uuid,
  authorization_id uuid,
  started_at     timestamptz not null default now(),
  ended_at       timestamptz,
  ip_address     inet,
  primary key (id, started_at)
) partition by range (started_at);
create table media.viewing_log_default partition of media.viewing_log default;


-- =============================================================================
-- Source: db/layer2_sped/10_ops.sql
-- =============================================================================

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


-- =============================================================================
-- Source: db/layer2_sped/11_camps.sql
-- =============================================================================

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


-- =============================================================================
-- Source: db/layer2_sped/12_consultancy.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 2 — School Consultancy
-- Reference (ref_value) categories: institution_type
-- =====================================================================

create table consultancy.institution (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  institution_type_id uuid references ref.ref_value(id),        -- ref_type 'institution_type'
  name           text not null,
  city           text,
  district       text,
  contact_name   text,
  contact_phone  text,
  contact_email  citext,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);

create table consultancy.consultancy_plan (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  institution_id uuid not null references consultancy.institution(id) on delete cascade,
  name           text not null,
  period_start   date,
  period_end     date,
  scope          text,
  lead_educator_id uuid references educators.educator(id),
  status         text not null default 'active' check (status in ('draft','active','completed','cancelled')),
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);

create table consultancy.school_visit (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  consultancy_plan_id uuid references consultancy.consultancy_plan(id) on delete cascade,
  institution_id uuid not null references consultancy.institution(id),
  visit_date     date not null,
  visitor_id     uuid references educators.educator(id),
  purpose        text,
  status         text not null default 'planned' check (status in ('planned','completed','cancelled')),
  created_at     timestamptz not null default now()
);

create table consultancy.observation_record (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  school_visit_id uuid not null references consultancy.school_visit(id) on delete cascade,
  subject        text,                                          -- class/teacher/child observed
  observation    text not null,
  recommendations text,
  created_at     timestamptz not null default now(),
  created_by     uuid
);

create table consultancy.consultancy_report (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  consultancy_plan_id uuid references consultancy.consultancy_plan(id),
  school_visit_id uuid references consultancy.school_visit(id),
  title          text not null,
  summary        text,
  file_id        uuid references core.file_object(id),
  authored_by    uuid,
  created_at     timestamptz not null default now()
);


-- =============================================================================
-- Source: db/layer2_sped/13_parent_portal.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: Layer 2 — Parent Portal (access layer + read views)
-- The portal is mostly an authorization/projection over existing domains.
-- Visibility is governed by: guardian linkage, portal_access flag, parent_visible
-- flags, and approved-status gates. RLS on base tables still applies.
-- =====================================================================

create table students.guardian_portal_access (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  guardian_id    uuid not null references students.guardian(id) on delete cascade,
  student_id     uuid not null references students.student(id) on delete cascade,
  can_view_sessions    boolean not null default true,
  can_view_attendance  boolean not null default true,
  can_view_reports     boolean not null default true,
  can_view_plan        boolean not null default true,
  can_view_finance     boolean not null default true,
  can_view_camera      boolean not null default false,
  granted_at     timestamptz not null default now(),
  revoked_at     timestamptz,
  unique (guardian_id, student_id)
);
comment on table students.guardian_portal_access is 'Per-(guardian,student) portal visibility switches. Camera defaults OFF (consent required).';

-- Students the CURRENT portal user (a guardian) may access.
create or replace view students.v_portal_my_students as
select sga.student_id,
       sga.guardian_id,
       sga.can_view_sessions, sga.can_view_attendance, sga.can_view_reports,
       sga.can_view_plan, sga.can_view_finance, sga.can_view_camera
from students.guardian_portal_access sga
join students.guardian g on g.id = sga.guardian_id
where sga.revoked_at is null
  and g.user_id = core.current_user_id();

-- Sessions visible to the portal user (their accessible students' sessions).
create or replace view scheduling.v_portal_sessions as
select sess.id      as session_id,
       sp.student_id as student_id,
       sess.title,
       sess.starts_at,
       sess.ends_at,
       sess.status
from scheduling.session sess
join scheduling.session_participant sp on sp.session_id = sess.id
where sess.deleted_at is null
  and exists (select 1 from students.v_portal_my_students m
              where m.student_id = sp.student_id and m.can_view_sessions);

-- Package balances visible to the portal user (derived from the credit ledger).
create or replace view finance.v_portal_package_balance as
select pkg.id as student_package_id,
       pkg.student_id,
       pkg.expires_on,
       pkg.total_credits,
       coalesce(sum(l.delta), 0) as remaining_credits,
       pkg.status
from finance.student_package pkg
left join finance.credit_ledger l on l.student_package_id = pkg.id
where exists (select 1 from students.v_portal_my_students m
              where m.student_id = pkg.student_id and m.can_view_finance)
group by pkg.id, pkg.student_id, pkg.expires_on, pkg.total_credits, pkg.status;

-- Approved education plans visible to guardians.
create or replace view education.v_portal_education_plan as
select ep.id, ep.student_id, ep.title, ep.version, ep.status, ep.effective_from, ep.effective_to
from education.education_plan ep
where ep.status = 'approved' and ep.guardian_visible = true
  and exists (select 1 from students.v_portal_my_students m
              where m.student_id = ep.student_id and m.can_view_plan);


-- =============================================================================
-- Source: db/99_triggers_rls_policies.sql
-- =============================================================================

-- =====================================================================
-- AyNesil Platform :: 99 — Cross-cutting wiring (run LAST)
-- 1) updated_at/row_version triggers   2) RLS tenant isolation
-- 3) audit triggers on sensitive data  4) educator scheduling-conflict guard
-- 5) log-table read/write policies
-- NOTE: migrations/seeds run as the table OWNER and bypass RLS (RLS is enabled,
--       not FORCED). The application MUST connect as a separate, non-owner role.
-- =====================================================================

-- Schemas in scope for generic wiring.
-- (kept inline in each DO block below)

-- ---------------------------------------------------------------------
-- 1) Attach updated_at/row_version maintenance to every table with updated_at
-- ---------------------------------------------------------------------
do $$
declare r record;
begin
  for r in
    select n.nspname as sch, c.relname as tbl
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where c.relkind = 'r'
      and n.nspname in ('core','iam','ref','crm','students','assessment','educators',
                        'education','scheduling','finance','legal','media','ops','camps','consultancy')
      and right(c.relname, 8) <> '_default'
      and exists (select 1 from pg_attribute a
                  where a.attrelid = c.oid and a.attname = 'updated_at' and not a.attisdropped)
  loop
    execute format(
      'create or replace trigger trg_set_updated_at before update on %I.%I
         for each row execute function core.set_updated_at()', r.sch, r.tbl);
  end loop;
end $$;

-- ---------------------------------------------------------------------
-- 2) Row-Level Security: tenant isolation on every table with corporation_id
--    (excludes append-only/system tables handled in section 5)
-- ---------------------------------------------------------------------
do $$
declare r record;
begin
  for r in
    select n.nspname as sch, c.relname as tbl
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where c.relkind = 'r'
      and n.nspname in ('core','iam','ref','crm','students','assessment','educators',
                        'education','scheduling','finance','legal','media','ops','camps','consultancy')
      and right(c.relname, 8) <> '_default'
      and not (n.nspname = 'core' and c.relname in ('outbox_event'))
      and exists (select 1 from pg_attribute a
                  where a.attrelid = c.oid and a.attname = 'corporation_id' and not a.attisdropped)
  loop
    execute format('alter table %I.%I enable row level security', r.sch, r.tbl);
    execute format('drop policy if exists tenant_isolation on %I.%I', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation on %I.%I
         using (corporation_id is null or corporation_id = core.current_corporation_id())
         with check (corporation_id = core.current_corporation_id())', r.sch, r.tbl);
  end loop;
end $$;

-- ---------------------------------------------------------------------
-- 3) Generic audit trigger on clinical / financial / legal / scheduling data
-- ---------------------------------------------------------------------
do $$
declare r record;
begin
  for r in
    select n.nspname as sch, c.relname as tbl
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where c.relkind = 'r'
      and n.nspname in ('students','assessment','education','scheduling','finance','legal','media')
      and right(c.relname, 8) <> '_default'
  loop
    execute format(
      'create or replace trigger trg_audit after insert or update or delete on %I.%I
         for each row execute function core.audit_trigger()', r.sch, r.tbl);
  end loop;
end $$;

-- ---------------------------------------------------------------------
-- 4) Educator scheduling-conflict guard (multi-educator sessions can't use a
--    single EXCLUDE constraint, so enforce overlap via trigger)
-- ---------------------------------------------------------------------
create or replace function scheduling.check_educator_overlap()
returns trigger
language plpgsql
as $$
begin
  if exists (
    select 1
    from scheduling.session_educator se
    join scheduling.session s2 on s2.id = se.session_id
    join scheduling.session s1 on s1.id = new.session_id
    where se.educator_id = new.educator_id
      and se.session_id <> new.session_id
      and s1.time_range && s2.time_range
      and s1.status <> 'cancelled' and s1.deleted_at is null
      and s2.status <> 'cancelled' and s2.deleted_at is null
  ) then
    raise exception 'Educator % is already booked in an overlapping session', new.educator_id
      using errcode = 'exclusion_violation';
  end if;
  return new;
end;
$$;

create or replace trigger trg_educator_overlap
  before insert or update on scheduling.session_educator
  for each row execute function scheduling.check_educator_overlap();

-- ---------------------------------------------------------------------
-- 5) Log / append-only tables: tenant-scoped reads, unrestricted writes
--    (written by triggers/system processes; read via reporting layer)
-- ---------------------------------------------------------------------
do $$
declare r record;
begin
  for r in
    select * from (values
      ('core','audit_log'), ('core','activity_log'), ('core','integration_log'),
      ('media','viewing_log')
    ) as t(sch, tbl)
  loop
    execute format('alter table %I.%I enable row level security', r.sch, r.tbl);
    execute format('drop policy if exists log_read on %I.%I',  r.sch, r.tbl);
    execute format('drop policy if exists log_write on %I.%I', r.sch, r.tbl);
    execute format(
      'create policy log_read on %I.%I for select
         using (corporation_id is null or corporation_id = core.current_corporation_id())',
      r.sch, r.tbl);
    execute format(
      'create policy log_write on %I.%I for insert with check (true)',
      r.sch, r.tbl);
  end loop;
end $$;

