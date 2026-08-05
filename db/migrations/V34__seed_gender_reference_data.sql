-- =====================================================================
-- Seed gender reference type + values (used by student form dropdown).
-- student.gender is free text storing the ref_value.code.
-- Idempotent.
-- =====================================================================

insert into ref.ref_type (code, name, is_system, is_hierarchical, allows_tenant_values)
values
  ('gender', 'Genders', true, false, false)
on conflict (code) do nothing;

insert into ref.ref_value (ref_type_id, code, sort_order, is_default, is_system)
select ref.type_id(v.type_code), v.code, v.sort_order, v.is_default, v.is_system
from (values
  ('gender', 'male', 1, false, true),
  ('gender', 'female', 2, false, true),
  ('gender', 'unspecified', 3, true, true)
) as v (type_code, code, sort_order, is_default, is_system)
where ref.type_id(v.type_code) is not null
on conflict do nothing;

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  ('gender', 'male', 'tr', 'Erkek'),
  ('gender', 'male', 'en', 'Male'),
  ('gender', 'female', 'tr', 'Kadın'),
  ('gender', 'female', 'en', 'Female'),
  ('gender', 'unspecified', 'tr', 'Belirtilmedi'),
  ('gender', 'unspecified', 'en', 'Unspecified')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;
