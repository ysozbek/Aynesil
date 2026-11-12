-- =====================================================================
-- Akran Platform :: DDL validation checks (run after all DDL + seed)
-- Prints a summary, then asserts hard expectations (raises => psql exits non-zero).
-- =====================================================================
\pset pager off

\echo ''
\echo '== Extensions =='
select extname, extversion from pg_extension
where extname in ('pgcrypto','btree_gist','pg_trgm','unaccent','citext')
order by extname;

\echo ''
\echo '== Tables per schema (relkind r/p, excluding partitions) =='
select n.nspname as schema, count(*) as tables
from pg_class c join pg_namespace n on n.oid = c.relnamespace
where n.nspname in ('core','iam','ref','crm','students','assessment','educators',
                    'education','scheduling','finance','legal','media','ops','camps','consultancy')
  and c.relkind in ('r','p') and not c.relispartition
group by n.nspname order by n.nspname;

\echo ''
\echo '== Totals =='
select
  (select count(*) from information_schema.schemata
     where schema_name in ('core','iam','ref','crm','students','assessment','educators',
                           'education','scheduling','finance','legal','media','ops','camps','consultancy')) as schemas,
  (select count(*) from pg_class c join pg_namespace n on n.oid=c.relnamespace
     where n.nspname in ('core','iam','ref','crm','students','assessment','educators',
                         'education','scheduling','finance','legal','media','ops','camps','consultancy')
       and c.relkind in ('r','p') and not c.relispartition) as tables,
  (select count(*) from pg_class c join pg_namespace n on n.oid=c.relnamespace
     where c.relkind='v'
       and n.nspname in ('core','iam','ref','crm','students','assessment','educators',
                         'education','scheduling','finance','legal','media','ops','camps','consultancy')) as views,
  (select count(*) from pg_policies where policyname='tenant_isolation') as tenant_policies,
  (select count(*) from pg_policies) as total_policies,
  (select count(*) from pg_class where relrowsecurity) as rls_tables,
  (select count(*) from pg_constraint where contype='x') as exclusion_constraints,
  (select count(*) from ref.ref_type) as ref_types,
  (select count(*) from ref.ref_value) as ref_values;

\echo ''
\echo '== Exclusion constraints =='
select conrelid::regclass as table, conname from pg_constraint where contype='x' order by 1;

\echo ''
-- ---------------------------------------------------------------------
-- #86 tenant integrity (db/98_tenant_integrity.sql, migrations V38–V41)
-- ---------------------------------------------------------------------
\echo '== Tenant integrity =='
do $$
declare
  v_fk   int;
  v_left int;
  v_trg  int;
begin
  -- every FK between two tenant tables is a validated composite (corporation_id, x) FK
  select count(*) into v_fk
    from pg_constraint where contype = 'f' and conname like 'fk\_%\_tnt%' and convalidated;
  select count(*) into v_left
    from pg_constraint c
    join pg_attribute a  on a.attrelid = c.conrelid  and a.attnum = c.conkey[1]
    join pg_attribute af on af.attrelid = c.confrelid and af.attnum = c.confkey[1]
   where c.contype = 'f' and c.conparentid = 0 and cardinality(c.conkey) = 1
     and af.attname = 'id' and a.attname <> 'corporation_id'
     and exists (select 1 from pg_attribute x where x.attrelid = c.conrelid  and x.attname = 'corporation_id' and x.attnotnull)
     and exists (select 1 from pg_attribute y where y.attrelid = c.confrelid and y.attname = 'corporation_id' and y.attnotnull);
  select count(*) into v_trg
    from pg_trigger where not tgisinternal and tgfoid = 'core.enforce_global_or_tenant_ref'::regproc;

  if v_fk < 202 then raise exception 'FAIL tenant FKs: expected >= 202 validated composite FKs, got %', v_fk; end if;
  if v_left > 0 then raise exception 'FAIL tenant FKs: % single-column FK(s) between tenant tables remain', v_left; end if;
  if v_trg < 63 then raise exception 'FAIL tenant scope triggers: expected >= 63, got %', v_trg; end if;
  raise notice 'TENANT INTEGRITY OK (composite_fks=%, scope_triggers=%)', v_fk, v_trg;
end $$;

\echo '== Assertions =='
do $$
declare
  v_ext int; v_sch int; v_tab int; v_excl int; v_rls boolean; v_types int; v_pol int;
begin
  select count(*) into v_ext from pg_extension
    where extname in ('pgcrypto','btree_gist','pg_trgm','unaccent','citext');
  if v_ext <> 5 then raise exception 'FAIL extensions: expected 5, got %', v_ext; end if;

  select count(*) into v_sch from information_schema.schemata
    where schema_name in ('core','iam','ref','crm','students','assessment','educators',
                          'education','scheduling','finance','legal','media','ops','camps','consultancy');
  if v_sch <> 15 then raise exception 'FAIL schemas: expected 15, got %', v_sch; end if;

  select count(*) into v_tab from pg_class c join pg_namespace n on n.oid=c.relnamespace
    where n.nspname in ('core','iam','ref','crm','students','assessment','educators',
                        'education','scheduling','finance','legal','media','ops','camps','consultancy')
      and c.relkind in ('r','p') and not c.relispartition;
  -- No fixed table count: completeness is checked by drift.sql, which compares this
  -- layer-built schema with the Flyway-built one (tables, columns, FKs, unique indexes, triggers).

  select count(*) into v_excl from pg_constraint where contype='x';
  if v_excl <> 2 then raise exception 'FAIL exclusion constraints: expected 2, got %', v_excl; end if;

  select c.relrowsecurity into v_rls from pg_class c join pg_namespace n on n.oid=c.relnamespace
    where n.nspname='students' and c.relname='student';
  if not coalesce(v_rls,false) then raise exception 'FAIL: RLS not enabled on students.student'; end if;

  if not exists (select 1 from pg_policies
                 where schemaname='students' and tablename='student' and policyname='tenant_isolation') then
    raise exception 'FAIL: tenant_isolation policy missing on students.student';
  end if;

  select count(*) into v_types from ref.ref_type;
  if v_types < 42 then raise exception 'FAIL: expected >= 42 seeded ref_types, got %', v_types; end if;

  -- Phase 3: user_can_access_student() must exist as the real production function
  if not exists (
    select 1 from pg_proc p
    join pg_namespace n on n.oid = p.pronamespace
    where n.nspname = 'students' and p.proname = 'user_can_access_student'
  ) then
    raise exception 'FAIL: students.user_can_access_student() function missing (Phase 3 not applied?)';
  end if;

  -- Phase 3: at least 17 RESTRICTIVE care_team_isolation policies must exist
  select count(*) into v_pol from pg_policies where policyname = 'care_team_isolation';
  if v_pol < 17 then
    raise exception 'FAIL: expected >= 17 care_team_isolation policies, got % (Phase 3 not applied?)', v_pol;
  end if;

  raise notice 'ALL ASSERTIONS PASSED (extensions=%, schemas=%, tables=%, exclusion=%, ref_types=%, care_team_policies=%)',
    v_ext, v_sch, v_tab, v_excl, v_types, v_pol;
end $$;
