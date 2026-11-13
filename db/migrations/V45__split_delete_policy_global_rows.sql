-- =============================================================================
-- Aynesil Flyway V45 — global satırlar kiracı tarafından silinemez (DELETE ayrı politika)
-- =============================================================================
-- tenant_isolation (FOR ALL) USING dalı `corporation_id IS NULL` içerdiği için DELETE,
-- global (platform) satırlara da ulaşıyordu. Politika komut başına bölünür:
--   tenant_isolation          FOR SELECT  — global + kendi satırları
--   tenant_isolation_insert   FOR INSERT  — yalnızca kendi kurumu
--   tenant_isolation_update   FOR UPDATE  — global + kendi satırları hedeflenebilir, sonuç kendi kurumu olmalı
--   tenant_isolation_delete   FOR DELETE  — yalnızca corporation_id = mevcut kurum
-- Katalogdan üretilir (idempotent: bölünmüş politika artık cmd='*' olmadığından yeniden eşleşmez).
-- iam.menu_item: global satırlar kiracı için salt okunur (UPDATE politikası sıkılaştırıldı).
-- db/99_triggers_rls_policies.sql aynı sonucu doğrudan üretir.
-- =============================================================================

do $$
declare r record;
begin
  for r in
    select n.nspname as sch, c.relname as tbl
      from pg_policy p
      join pg_class c on c.oid = p.polrelid
      join pg_namespace n on n.oid = c.relnamespace
     where p.polname = 'tenant_isolation'
       and p.polcmd = '*'
       and pg_get_expr(p.polqual, p.polrelid) like '%corporation_id IS NULL%'
  loop
    execute format('drop policy tenant_isolation on %I.%I', r.sch, r.tbl);
    execute format('drop policy if exists tenant_isolation_insert on %I.%I', r.sch, r.tbl);
    execute format('drop policy if exists tenant_isolation_update on %I.%I', r.sch, r.tbl);
    execute format('drop policy if exists tenant_isolation_delete on %I.%I', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation on %I.%I for select
         using (corporation_id is null or corporation_id = core.current_corporation_id())', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation_insert on %I.%I for insert
         with check (corporation_id = core.current_corporation_id())', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation_update on %I.%I for update
         using (corporation_id is null or corporation_id = core.current_corporation_id())
         with check (corporation_id = core.current_corporation_id())', r.sch, r.tbl);
    execute format(
      'create policy tenant_isolation_delete on %I.%I for delete
         using (corporation_id = core.current_corporation_id())', r.sch, r.tbl);
  end loop;
end $$;

-- iam.menu_item: platform varsayılanları (corporation_id IS NULL) kiracı için salt okunur.
drop policy if exists menu_item_update on iam.menu_item;
create policy menu_item_update on iam.menu_item
  for update
  using (corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());
