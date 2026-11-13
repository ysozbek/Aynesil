-- =============================================================================
-- Manual rollback for V45 (DELETE split). NOT run by Flyway; run as the owner role, then delete
-- the V45 row from flyway_schema_history. Restores the single FOR ALL tenant_isolation policy —
-- which lets tenants delete global rows again — and the permissive menu_item_update.
-- =============================================================================
begin;
do $$
declare r record;
begin
  for r in
    select n.nspname as sch, c.relname as tbl
      from pg_policy p join pg_class c on c.oid = p.polrelid join pg_namespace n on n.oid = c.relnamespace
     where p.polname = 'tenant_isolation_delete'
  loop
    execute format('drop policy tenant_isolation on %I.%I', r.sch, r.tbl);
    execute format('drop policy tenant_isolation_insert on %I.%I', r.sch, r.tbl);
    execute format('drop policy tenant_isolation_update on %I.%I', r.sch, r.tbl);
    execute format('drop policy tenant_isolation_delete on %I.%I', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation on %I.%I
         using (corporation_id is null or corporation_id = core.current_corporation_id())
         with check (corporation_id = core.current_corporation_id())', r.sch, r.tbl);
  end loop;
end $$;
drop policy if exists menu_item_update on iam.menu_item;
create policy menu_item_update on iam.menu_item for update
  using (corporation_id is null or corporation_id = core.current_corporation_id())
  with check (corporation_id is null or corporation_id = core.current_corporation_id());
commit;
