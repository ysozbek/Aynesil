-- =============================================================================
-- Tenant: Akran Hareket — kuruma özel ek veri (idempotent; create.sh çalıştırır)
-- Eskiden V2 migration'ındaki örnek kurum bloğundaydı; platform migration'larından çıkarıldı.
--   1. Kampüs ilçeleri (new_tenant.py yalnızca ad ve şehir alır)
--   2. Kuruma özel referans değerleri: hidroterapi (terapi türü), ev ziyareti (seans türü)
--   3. Global bir değerin kurum için kapatılması: 'online' seans türü
-- Sahip rolüyle çalışır (RLS'yi aşar). Kurum yoksa hiçbir şey yapmaz.
-- =============================================================================

update core.campus cp
   set district = v.district, updated_at = now()
  from core.corporation c,
       (values ('Etiler Kampüsü', 'Beşiktaş'), ('Kadıköy Kampüsü', 'Kadıköy'), ('Ankara Kampüsü', 'Çankaya'),
               ('Bahçelievler Kampüsü', 'Bahçelievler'), ('Eyüpsultan Kampüsü', 'Eyüpsultan'),
               ('Mecidiyeköy Kampüsü', 'Şişli'), ('Bayrampaşa Kampüsü', 'Bayrampaşa'),
               ('Bahçeşehir Kampüsü', 'Küçükçekmece'), ('Yalova Kampüsü', 'Merkez'),
               ('Çekmeköy Kampüsü', 'Çekmeköy'), ('Ordu Fatsa Kampüsü', 'Fatsa')) as v(name, district)
 where c.code = 'akran' and cp.corporation_id = c.id and cp.name = v.name
   and cp.district is distinct from v.district;

insert into ref.ref_value(ref_type_id, corporation_id, code, sort_order, is_active)
select ref.type_id(v.type_code), c.id, v.code, v.sort_order, true
from core.corporation c,
     (values ('therapy_type','hydrotherapy',7),
             ('session_type','home_visit',6)) as v(type_code, code, sort_order)
where c.code = 'akran'
on conflict do nothing;

insert into ref.ref_value_translation(ref_value_id, locale, label)
select rv.id, t.locale, t.label
from core.corporation c
join ref.ref_value rv on rv.corporation_id = c.id
join (values ('therapy_type','hydrotherapy','tr','Hidroterapi'),
             ('therapy_type','hydrotherapy','en','Hydrotherapy'),
             ('session_type','home_visit','tr','Ev Ziyareti'),
             ('session_type','home_visit','en','Home Visit'))
       as t(type_code, value_code, locale, label)
  on rv.ref_type_id = ref.type_id(t.type_code) and rv.code = t.value_code
where c.code = 'akran'
on conflict (ref_value_id, locale) do nothing;

insert into ref.ref_value_tenant_override(corporation_id, ref_value_id, is_active, sort_order)
select c.id, rv.id, false, null
from core.corporation c
join ref.ref_value rv on rv.ref_type_id = ref.type_id('session_type')
                     and rv.code = 'online' and rv.corporation_id is null
where c.code = 'akran'
on conflict (corporation_id, ref_value_id) do nothing;
