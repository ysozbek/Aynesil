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
