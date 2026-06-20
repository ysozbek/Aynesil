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
