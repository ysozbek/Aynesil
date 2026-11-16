-- =====================================================================
-- V59 — Bakım ekibi menü satırı açılıyor
--
-- V49 "students-care-team" satırını ekran yok diye kapatmıştı. Ekranlar geldi:
--   • öğrenci detayında "Bakım Ekibi" sekmesi (ekip yönetimi, care_team:read / :assign)
--   • "Benim Öğrencilerim" sayfası  /care-team/my-students  (eğitmenin kendi ekibindeki öğrenciler)
-- Menü satırı bu ikinci sayfaya gider; izni care_team:read'e iner (eğitmen de görebilsin).
-- feature_flag (V53: 'care_team') değişmez: modül kapalıysa satır menüden düşer.
-- Şema değişikliği yok; yalnızca veri. Idempotent.
-- =====================================================================

update iam.menu_item
   set route               = '/care-team/my-students',
       required_permission_id = (select id from iam.permission where code = 'care_team:read'),
       is_active           = true,
       updated_at          = now()
 where corporation_id is null
   and code = 'students-care-team';

update iam.menu_item_translation t
   set label = v.label
  from iam.menu_item m
  join (values ('tr', 'Benim Öğrencilerim'), ('en', 'My Students')) as v (locale, label) on true
 where t.menu_item_id = m.id
   and m.corporation_id is null
   and m.code = 'students-care-team'
   and t.locale = v.locale
   and t.label is distinct from v.label;
