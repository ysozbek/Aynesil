-- =============================================================================
-- AyNesil Platform :: Flyway V34 — menu_item RLS for platform defaults
-- =============================================================================
-- Generic tenant_isolation WITH CHECK requires corporation_id = current tenant.
-- Platform menu rows have corporation_id IS NULL, so SELECT works (USING allows
-- null) but UPDATE/activate/deactivate/reorder fail WITH CHECK.
--
-- Split policies:
--   SELECT  — platform defaults + own tenant rows
--   INSERT  — tenant-scoped rows only (cannot invent platform defaults)
--   UPDATE  — may update platform defaults (keep null) or own rows
--   DELETE  — tenant-scoped rows only (platform defaults are deactivate-only)
-- =============================================================================

drop policy if exists tenant_isolation on iam.menu_item;

create policy menu_item_select on iam.menu_item
  for select
  using (corporation_id is null or corporation_id = core.current_corporation_id());

create policy menu_item_insert on iam.menu_item
  for insert
  with check (corporation_id = core.current_corporation_id());

create policy menu_item_update on iam.menu_item
  for update
  using (corporation_id is null or corporation_id = core.current_corporation_id())
  with check (corporation_id is null or corporation_id = core.current_corporation_id());

create policy menu_item_delete on iam.menu_item
  for delete
  using (corporation_id = core.current_corporation_id());
