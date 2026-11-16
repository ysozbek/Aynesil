-- =====================================================================
-- V61 — Anket modülü menü satırları
--
-- Ekranlar geldi: anket listesi/oluşturma/düzenleme/sonuçlar (/surveys...), veli geri bildirim listesi
-- (/surveys/feedback) ve veli portalı sayfaları (/portal/surveys, /portal/feedback).
--   • V17'deki "students-surveys" ve "students-feedback" satırları (V31'de pasifleştirilmişti) açılır,
--     rotaları gerçek sayfalara çevrilir.
--   • Portal altına "Anketler" ve "Geri Bildirim" satırları eklenir (veli rolünün izinleri:
--     survey:respond, parent_feedback:create).
-- Hepsi feature_flag = 'surveys' (V53): modül kapalıysa menüden düşer.
-- Şema değişikliği yok; yalnızca veri. Idempotent.
-- =====================================================================

update iam.menu_item m
   set route               = v.route,
       is_active           = true,
       feature_flag        = 'surveys',
       required_permission_id = (select id from iam.permission where code = v.perm_code),
       updated_at          = now()
  from (values
        ('students-surveys',  '/surveys',          'survey:read'),
        ('students-feedback', '/surveys/feedback', 'parent_feedback:read')
       ) as v (code, route, perm_code)
 where m.corporation_id is null
   and m.code = v.code;

insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active, feature_flag)
select null, parent.id, v.code, v.route, v.icon, v.sort_order, p.id, true, 'surveys'
  from (values
        ('portal-surveys',  '/portal/surveys',  'clipboard-list', 40, 'survey:respond'),
        ('portal-feedback', '/portal/feedback', 'star',           50, 'parent_feedback:create')
       ) as v (code, route, icon, sort_order, perm_code)
  join iam.menu_item parent on parent.code = 'portal' and parent.corporation_id is null
  left join iam.permission p on p.code = v.perm_code
on conflict (corporation_id, code) do nothing;

update iam.menu_item
   set feature_flag = 'surveys', is_active = true
 where corporation_id is null
   and code in ('portal-surveys', 'portal-feedback')
   and (feature_flag is distinct from 'surveys' or not is_active);

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
  from iam.menu_item m
  join (values
        ('portal-surveys',  'tr', 'Anketler'),
        ('portal-surveys',  'en', 'Surveys'),
        ('portal-feedback', 'tr', 'Geri Bildirim'),
        ('portal-feedback', 'en', 'Feedback')
       ) as t (code, locale, label) on t.code = m.code
 where m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
