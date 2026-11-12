-- =====================================================================
-- Schema drift check: layer DDL + seed (database `akran`) vs. Flyway migrations
-- (database `akran_flyway`), both built in the same PG17 container by smoke_test.sh.
--
-- Runs in a scratch database (`akran_drift`) so dblink is never installed in the compared
-- schemas. Compares, by definition rather than by object name:
--   tables      schema.table + relkind
--   columns     type (format_type) + nullability
--   foreign keys referencing/referenced columns + ON DELETE / ON UPDATE / deferrable
--   unique idx  normalized index definition (name removed; includes partial predicates)
--   triggers    normalized trigger definition (timing, events, function, arguments)
--   rls         relrowsecurity / relforcerowsecurity per table
--   policies    command, permissive/restrictive, roles, USING and WITH CHECK expressions
-- Every difference is listed; the script fails if there is at least one.
--
-- It then checks an invariant in EACH database (so it also catches a defect that both
-- schemas share): every table with a corporation_id column has RLS enabled and a
-- tenant_isolation policy, except the deliberate exceptions listed in
-- tenant_rls_exceptions below — and those are verified against their own expected setup.
--
-- Intentionally excluded (present only in the migration-built database by design):
--   public.flyway_schema_history  Flyway's own bookkeeping table
--   schema hangfire               Hangfire job store (V3), owned by the job runtime, not domain DDL
-- =====================================================================
\set ON_ERROR_STOP 1
create extension if not exists dblink;

create temp table snapshot_sql as select $snap$
  with rel as (
    select c.oid, n.nspname as sch, c.relname as tbl, c.relkind
      from pg_class c join pg_namespace n on n.oid = c.relnamespace
     where c.relkind in ('r', 'p')
       and n.nspname not in ('pg_catalog', 'information_schema', 'pg_toast', 'hangfire')
       and n.nspname not like 'pg\_temp%' and n.nspname not like 'pg\_toast\_temp%'
       and not (n.nspname = 'public' and c.relname = 'flyway_schema_history')
  )
  select 'table'::text as kind, sch || '.' || tbl as obj, relkind::text as detail from rel
  union all
  select 'column', r.sch || '.' || r.tbl || '.' || a.attname,
         format_type(a.atttypid, a.atttypmod) || case when a.attnotnull then ' not null' else ' null' end
    from rel r join pg_attribute a on a.attrelid = r.oid and a.attnum > 0 and not a.attisdropped
  union all
  select 'foreign key',
         r.sch || '.' || r.tbl || ' (' ||
           (select string_agg(a.attname, ', ' order by k.ord) from unnest(c.conkey) with ordinality k(attnum, ord)
              join pg_attribute a on a.attrelid = c.conrelid and a.attnum = k.attnum) || ') -> ' ||
           c.confrelid::regclass::text || ' (' ||
           (select string_agg(a.attname, ', ' order by k.ord) from unnest(c.confkey) with ordinality k(attnum, ord)
              join pg_attribute a on a.attrelid = c.confrelid and a.attnum = k.attnum) || ')',
         'on delete ' || c.confdeltype::text ||
           coalesce(' (' || (select string_agg(a.attname, ', ') from unnest(c.confdelsetcols) s(attnum)
              join pg_attribute a on a.attrelid = c.conrelid and a.attnum = s.attnum) || ')', '') ||
           ', on update ' || c.confupdtype::text ||
           case when c.condeferrable then ', deferrable' else '' end
    from rel r join pg_constraint c on c.conrelid = r.oid and c.contype = 'f' and c.conparentid = 0
  union all
  select 'unique index', r.sch || '.' || r.tbl,
         regexp_replace(pg_get_indexdef(i.indexrelid), 'INDEX \S+ ON ', 'INDEX ON ')
    from rel r join pg_index i on i.indrelid = r.oid and i.indisunique
  union all
  select 'trigger', r.sch || '.' || r.tbl || '.' || t.tgname,
         regexp_replace(pg_get_triggerdef(t.oid), '^CREATE (OR REPLACE )?(CONSTRAINT )?TRIGGER \S+ ', '')
    from rel r join pg_trigger t on t.tgrelid = r.oid and not t.tgisinternal
  union all
  select 'rls', r.sch || '.' || r.tbl,
         'enabled=' || c.relrowsecurity::text || ', forced=' || c.relforcerowsecurity::text
    from rel r join pg_class c on c.oid = r.oid
  union all
  select 'policy', p.schemaname || '.' || p.tablename || '.' || p.policyname,
         p.cmd || ' ' || p.permissive || ' to ' || array_to_string(p.roles, ',') ||
           ' using (' || coalesce(p.qual, '-') || ') with check (' || coalesce(p.with_check, '-') || ')'
    from pg_policies p join rel r on r.sch = p.schemaname and r.tbl = p.tablename
$snap$ as q;

create temp table snap_layer as
  select * from dblink('dbname=akran user=postgres', (select q from snapshot_sql))
    as t(kind text, obj text, detail text);
create temp table snap_flyway as
  select * from dblink('dbname=akran_flyway user=postgres', (select q from snapshot_sql))
    as t(kind text, obj text, detail text);

create temp table drift as
  select 'only in layer DDL' as side, * from (select * from snap_layer except select * from snap_flyway) a
  union all
  select 'only in migrations', * from (select * from snap_flyway except select * from snap_layer) b;

\echo '== Schema drift (layer DDL vs Flyway migrations) =='
select kind, count(*) filter (where true) as objects_compared
  from snap_flyway group by kind order by kind;

\pset pager off
select kind, obj, side, detail from drift order by kind, obj, side, detail;

-- ── Invariant: tenant tables are RLS-isolated (checked in each database) ───────
-- Deliberate exceptions, each with the setup it must have instead of tenant_isolation.
create temp table tenant_rls_exceptions (tbl text primary key, rls_enabled boolean, policies text, reason text);
insert into tenant_rls_exceptions values
  ('iam.menu_item',       true,  'menu_item_delete,menu_item_insert,menu_item_select,menu_item_update',
   'platform-default rows (corporation_id IS NULL) are readable/updatable by every tenant; split per-command policies (99)'),
  ('core.audit_log',      true,  'log_read,log_write', 'append-only log written by triggers; tenant-scoped reads only (99)'),
  ('core.activity_log',   true,  'log_read,log_write', 'append-only log; tenant-scoped reads only (99)'),
  ('core.integration_log',true,  'log_read,log_write', 'append-only log; tenant-scoped reads only (99)'),
  ('media.viewing_log',   true,  'log_read,log_write', 'append-only viewing audit trail; tenant-scoped reads only (99)'),
  ('core.outbox_event',   false, '',                   'system outbox drained by the background dispatcher across tenants; excluded from RLS in 99');

create temp table invariant_sql as select $inv$
  select c.oid::regclass::text as tbl, c.relrowsecurity as rls_enabled,
         coalesce((select string_agg(p.polname, ',' order by p.polname) from pg_policy p where p.polrelid = c.oid), '') as policies,
         exists (select 1 from pg_policy p where p.polrelid = c.oid and p.polname = 'tenant_isolation') as has_tenant_isolation
    from pg_class c join pg_namespace n on n.oid = c.relnamespace
   where c.relkind in ('r', 'p') and not c.relispartition
     and n.nspname not in ('pg_catalog', 'information_schema', 'hangfire', 'public')
     and exists (select 1 from pg_attribute a where a.attrelid = c.oid and a.attname = 'corporation_id' and not a.attisdropped)
$inv$ as q;

create temp table rls_violations as
  with tenant_tables as (
    select 'akran (layer DDL)' as db, * from dblink('dbname=akran user=postgres', (select q from invariant_sql))
      as t(tbl text, rls_enabled boolean, policies text, has_tenant_isolation boolean)
    union all
    select 'akran_flyway (migrations)', * from dblink('dbname=akran_flyway user=postgres', (select q from invariant_sql))
      as t(tbl text, rls_enabled boolean, policies text, has_tenant_isolation boolean)
  )
  select t.db, t.tbl,
         case
           when e.tbl is null and not t.rls_enabled          then 'RLS disabled'
           when e.tbl is null and not t.has_tenant_isolation then 'tenant_isolation policy missing (has: ' || nullif(t.policies, '') || ')'
           when e.tbl is not null and t.rls_enabled <> e.rls_enabled
             then 'exception expects rls_enabled=' || e.rls_enabled || ', got ' || t.rls_enabled
           when e.tbl is not null and t.policies <> e.policies
             then 'exception expects policies [' || e.policies || '], got [' || t.policies || ']'
         end as problem
    from tenant_tables t left join tenant_rls_exceptions e on e.tbl = t.tbl;
delete from rls_violations where problem is null;

\echo '== Invariant: every tenant table is RLS-isolated (both databases) =='
select db, tbl, problem from rls_violations order by db, tbl;

do $$
declare
  n int;
  v int;
begin
  select count(*) into v from rls_violations;
  if v > 0 then
    raise exception 'FAIL tenant RLS invariant: % table(s) without the expected isolation (listed above)', v;
  end if;
  raise notice 'TENANT RLS INVARIANT OK (exceptions: %)', (select string_agg(tbl, ', ' order by tbl) from tenant_rls_exceptions);

  select count(*) into n from drift;
  if n > 0 then
    raise exception 'FAIL schema drift: % difference(s) between layer DDL and Flyway migrations (listed above)', n;
  end if;
  raise notice 'NO SCHEMA DRIFT (layer DDL and Flyway migrations produce the same catalog)';
end $$;
