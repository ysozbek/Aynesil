-- =============================================================================
-- Tenant: Akran Hareket — RBAC bootstrap
-- Admin rolü + platform işlemleri (corporation:create/delete) dışındaki tüm permission'ların atanması.
-- V6 (platform permissions) uygulandıktan sonra çalıştırın.
-- Owner rolüyle çalıştırın.
-- =============================================================================

-- Admin rolü
insert into iam.role (corporation_id, code, name, is_system)
select c.id, 'admin', 'Administrator', true
from core.corporation c
where c.code = 'akran'
on conflict (corporation_id, code) do nothing;

-- Admin rolüne platform işlemleri dışındaki tüm permission'ları ver
insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.role r
join core.corporation c on c.id = r.corporation_id and c.code = 'akran'
cross join iam.permission p
where r.code = 'admin'
  -- Platform operations (Permissions.PlatformOnly, V43) are never tenant permissions.
  and p.code not in ('corporation:create', 'corporation:delete')
on conflict do nothing;
