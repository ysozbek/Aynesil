-- =====================================================================
-- V60 — Okul danışmanlığı "Raporlar ve Özetler" menü satırı
--
-- /consultancy/analytics sayfası eklendi: sözleşme özeti, okul (kurum) raporu, plan sonuçları ve
-- ziyaret geçmişi (ConsultancyController reporting/* uçları). Sayfa kurum yöneticisinin
-- institution:read iznine bağlı; sekmeler kendi uçlarının iznine göre görünür.
-- feature_flag = 'consultancy' (V53): modül kapalıysa satır menüden düşer.
-- Şema değişikliği yok; yalnızca veri. Idempotent.
-- =====================================================================

insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active, feature_flag)
select null, parent.id, 'consultancy.analytics', '/consultancy/analytics', 'chart', 8, p.id, true, 'consultancy'
  from iam.menu_item parent
  left join iam.permission p on p.code = 'institution:read'
 where parent.code = 'consultancy' and parent.corporation_id is null
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
  from iam.menu_item m
  join (values
        ('tr', 'Raporlar ve Özetler'),
        ('en', 'Reports & Summaries')
       ) as t (locale, label) on true
 where m.corporation_id is null
   and m.code = 'consultancy.analytics'
on conflict (menu_item_id, locale) do nothing;
