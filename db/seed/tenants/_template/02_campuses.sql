-- =============================================================================
-- Tenant Bootstrap Template — Campuses (branches)
-- =============================================================================

insert into core.campus (corporation_id, code, name, city, district, is_active)
select
  c.id,
  v.code,
  v.name,
  v.city,
  v.district,
  true
from core.corporation c,
     (values
       ('MRKZ', 'Merkez Kampüsü', '<Şehir>', '<İlçe>')
       -- ihtiyaç kadar satır ekleyin
     ) as v(code, name, city, district)
where c.code = '<tenant_code>'
on conflict (corporation_id, code) do nothing;
