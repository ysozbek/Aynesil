-- =============================================================================
-- AyNesil :: V43 — Y1: corporation management is tenant-scoped; create/activate/suspend
--                  are platform operations
-- =============================================================================
-- Y1 (Critical, docs/uyusmazlik-dogrulama.md): core.corporation had RLS disabled and the
-- corporation handlers did not filter by tenant, while every tenant admin role held
-- corporation:create/read/update/delete (V2/V27 + tenant seeds grant "every permission").
-- A tenant admin could list every customer and, by id, update, re-configure or suspend
-- another customer.
--
-- 1) corporation:create and corporation:delete (activate/suspend) are platform operations
--    (devops/new_tenant.py, run as the owner role). They are removed from every role; the
--    API rejects assigning them again (Permissions.PlatformOnly). Global roles are included:
--    a user can hold a global role, and there is no platform tier in the API.
--    corporation:read / corporation:update stay: a tenant reads and edits its OWN corporation.
--
-- 2) Defence in depth: core.corporation gets RLS with id = core.current_corporation_id().
--    Unaffected (they do not read core.corporation as aynesil_app without a tenant):
--      - login / token refresh: SECURITY DEFINER iam.authenticate_user / find_user_by_id /
--        find_session_by_token / get_user_permissions (owner role, bypass RLS);
--      - devops/new_tenant.py, seeds, Flyway: owner role (bypass RLS);
--      - FK checks from tenant tables to core.corporation (FK checks bypass RLS);
--      - StaleTenantMiddleware / ref.value_label(): read the session's own corporation.
--
-- Idempotent: DELETE is naturally repeatable; the policy is dropped/recreated.
-- =============================================================================

-- ── 1) Platform-only permissions out of every role ──────────────────────────
delete from iam.role_permission rp
 using iam.permission p
 where p.id = rp.permission_id
   and p.code in ('corporation:create', 'corporation:delete');

-- ── 2) core.corporation: a tenant sees and changes only its own row ─────────
alter table core.corporation enable row level security;
drop policy if exists tenant_isolation on core.corporation;
create policy tenant_isolation on core.corporation
  using (id = core.current_corporation_id())
  with check (id = core.current_corporation_id());
