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
