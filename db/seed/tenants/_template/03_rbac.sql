-- =============================================================================
-- Tenant Bootstrap Template — RBAC
-- V6 (platform permissions) uygulandıktan sonra çalıştırın.
-- =============================================================================

-- Admin rolü
insert into iam.role (corporation_id, code, name, is_system)
select c.id, 'admin', 'Administrator', true
from core.corporation c
where c.code = '<tenant_code>'
on conflict (corporation_id, code) do nothing;

-- Admin rolüne platform işlemleri dışındaki tüm permission'ları ver
insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.role r
join core.corporation c on c.id = r.corporation_id and c.code = '<tenant_code>'
cross join iam.permission p
where r.code = 'admin'
  -- Platform operations (Permissions.PlatformOnly, V43) are never tenant permissions.
  and p.code not in ('corporation:create', 'corporation:delete')
on conflict do nothing;

-- İsteğe bağlı: tenant-specific ek roller buraya eklenebilir
-- insert into iam.role ...
