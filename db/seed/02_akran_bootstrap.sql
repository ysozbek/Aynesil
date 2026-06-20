-- =====================================================================
-- AyNesil Platform :: Seed — Akran Hareket tenant bootstrap (example)
-- Demonstrates: corporation + campuses, base RBAC, tenant-specific reference
-- values, and a tenant override of a global value. Run as table owner.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Corporation + campuses
-- ---------------------------------------------------------------------
insert into core.corporation(code, legal_name, display_name, default_locale, default_currency, timezone)
values ('akran','Akran Hareket Özel Eğitim','Akran Hareket','tr','TRY','Europe/Istanbul')
on conflict (code) do nothing;

insert into core.campus(corporation_id, code, name, city)
select c.id, v.code, v.name, v.city
from core.corporation c,
     (values ('ETLK','Etiler Kampüs','İstanbul'),
             ('KDKY','Kadıköy Kampüs','İstanbul'),
             ('ANK','Ankara Kampüs','Ankara')) as v(code,name,city)
where c.code = 'akran'
on conflict (corporation_id, code) do nothing;

-- ---------------------------------------------------------------------
-- Permission catalog (sample) + admin role
-- ---------------------------------------------------------------------
insert into iam.permission(code, resource, action) values
  ('student:read','student','read'),
  ('student:write','student','write'),
  ('session:read','session','read'),
  ('session:write','session','write'),
  ('finance:read','finance','read'),
  ('finance:write','finance','write'),
  ('refdata:manage','refdata','manage')
on conflict (code) do nothing;

insert into iam.role(corporation_id, code, name, is_system)
select c.id, 'admin', 'Administrator', true
from core.corporation c where c.code = 'akran'
on conflict (corporation_id, code) do nothing;

insert into iam.role_permission(role_id, permission_id)
select r.id, p.id
from iam.role r
join core.corporation c on c.id = r.corporation_id and c.code = 'akran'
cross join iam.permission p
where r.code = 'admin'
on conflict do nothing;

-- ---------------------------------------------------------------------
-- Tenant-SPECIFIC reference values (only Akran sees these)
--   e.g. a custom therapy type + a custom session type
-- ---------------------------------------------------------------------
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

-- ---------------------------------------------------------------------
-- Tenant OVERRIDE of a GLOBAL value (deactivate 'online' session type for Akran,
-- and push 'group' to the top) WITHOUT mutating the shared rows.
-- ---------------------------------------------------------------------
insert into ref.ref_value_tenant_override(corporation_id, ref_value_id, is_active, sort_order)
select c.id, rv.id, false, null
from core.corporation c
join ref.ref_value rv on rv.ref_type_id = ref.type_id('session_type')
                     and rv.code = 'online' and rv.corporation_id is null
where c.code = 'akran'
on conflict (corporation_id, ref_value_id) do nothing;
