-- =============================================================================
-- AyNesil :: V42 — Fixes for migration-side schema defects found by the drift check
-- =============================================================================
-- 1) #87 (Critical): consultancy.consultancy_agreement and consultancy.follow_up_activity
--    were created by V26, which assumed 99_triggers_rls_policies.sql would add RLS and the
--    updated_at trigger "automatically". 99 only runs as part of the V1 baseline, so in every
--    migration-built database both tables had RLS disabled while aynesil_app could read them:
--    every tenant could see every tenant's agreements and follow-ups.
--    The policy and trigger below are identical to what 99 creates for all other tenant
--    tables (tenant_isolation / trg_set_updated_at).
--
-- 2) media.viewing_log.ip_address: V29 changed it to text, but the EF model maps
--    ViewingLog.IpAddress as System.Net.IPAddress with HasColumnType("inet"), so writes from
--    the API fail against a text column. Back to inet. viewing_log is partitioned; ALTER on
--    the parent rewrites all partitions. (auth_session / activity_log stay text: their
--    entities map string?, which is what V5/V29 aligned them to.)
--
-- Idempotent: policy is dropped/recreated, triggers use OR REPLACE, the type change is a
-- no-op rewrite when the column is already inet.
-- =============================================================================

-- ── 1) #87 consultancy RLS + updated_at ─────────────────────────────────────

alter table consultancy.consultancy_agreement enable row level security;
drop policy if exists tenant_isolation on consultancy.consultancy_agreement;
create policy tenant_isolation on consultancy.consultancy_agreement
  using (corporation_id is null or corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());
create or replace trigger trg_set_updated_at before update on consultancy.consultancy_agreement
  for each row execute function core.set_updated_at();

alter table consultancy.follow_up_activity enable row level security;
drop policy if exists tenant_isolation on consultancy.follow_up_activity;
create policy tenant_isolation on consultancy.follow_up_activity
  using (corporation_id is null or corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());
create or replace trigger trg_set_updated_at before update on consultancy.follow_up_activity
  for each row execute function core.set_updated_at();

-- ── 2) media.viewing_log.ip_address → inet ──────────────────────────────────

alter table media.viewing_log
  alter column ip_address type inet using ip_address::inet;
