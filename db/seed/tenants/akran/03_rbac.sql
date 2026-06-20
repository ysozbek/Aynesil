-- =============================================================================
-- Tenant: Akran Hareket — RBAC bootstrap
-- Admin rolü + tüm platform permission'larının atanması.
-- V6 (platform permissions) uygulandıktan sonra çalıştırın.
-- Owner rolüyle çalıştırın.
-- =============================================================================

-- Admin rolü
insert into iam.role (corporation_id, code, name, is_system)
select c.id, 'admin', 'Administrator', true
from core.corporation c
where c.code = 'akran'
on conflict (corporation_id, code) do nothing;

-- Admin rolüne tüm platform permission'larını ver
insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.role r
join core.corporation c on c.id = r.corporation_id and c.code = 'akran'
cross join iam.permission p
where r.code = 'admin'
on conflict do nothing;
