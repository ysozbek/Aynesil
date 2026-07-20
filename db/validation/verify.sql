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
  if v_tab <> 137 then raise exception 'FAIL tables: expected 137, got %', v_tab; end if;

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
