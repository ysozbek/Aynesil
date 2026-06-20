-- =============================================================================
-- Tenant: Akran Hareket — Campus (branch) bootstrap
-- Tüm şubeler burada tanımlanır. ON CONFLICT ile idempotent.
-- Owner rolüyle çalıştırın.
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
       -- V2'de seeded (ON CONFLICT ile atlanır ama burada da tanımlı — tek kaynak)
       ('ETLK',  'Etiler Kampüsü',          'İstanbul', 'Beşiktaş'),
       ('KDKY',  'Kadıköy Kampüsü',         'İstanbul', 'Kadıköy'),
       ('ANK',   'Ankara Kampüsü',          'Ankara',   'Çankaya'),
       -- Yeni şubeler
       ('BHCL',  'Bahçelievler Kampüsü',    'İstanbul', 'Bahçelievler'),
       ('EYPS',  'Eyüpsultan Kampüsü',      'İstanbul', 'Eyüpsultan'),
       ('MCDK',  'Mecidiyeköy Kampüsü',     'İstanbul', 'Şişli'),
       ('BYRP',  'Bayrampaşa Kampüsü',      'İstanbul', 'Bayrampaşa'),
       ('BHCS',  'Bahçeşehir Kampüsü',      'İstanbul', 'Küçükçekmece'),
       ('YLV',   'Yalova Kampüsü',          'Yalova',   'Merkez'),
       ('CKMK',  'Çekmeköy Kampüsü',        'İstanbul', 'Çekmeköy'),
       ('ODFT',  'Ordu Fatsa Kampüsü',      'Ordu',     'Fatsa')
     ) as v(code, name, city, district)
where c.code = 'akran'
on conflict (corporation_id, code) do nothing;
