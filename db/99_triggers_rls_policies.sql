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
