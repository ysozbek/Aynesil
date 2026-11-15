-- =====================================================================
-- V58 — Menü izinleri ekran izinleriyle hizalanır
--
-- 1) "Eğitimciler" (educators): V8 kök menüyü `user:read` ile açmıştı; V11 aynı kodu
--    `educator:read` ile eklemeye çalıştı ama ON CONFLICT DO NOTHING nedeniyle V8 satırı kaldı.
--    Sonuç: eğitmen listesini görme izni (educator:read) olan ama kullanıcı yönetimi izni
--    olmayan roller (Koordinatör, Eğitmen) menüde "Eğitimciler"i hiç görmüyordu. Alt menüler
--    (educators-list: educator:read) ve ekran (/educators) zaten educator:read ister.
-- 2) "KPI & Performans" (performance, /kpi): kök menü `report:read` istiyordu, ama /kpi sayfası
--    (KPI panosu) ve API'si `kpi_dashboard:read` ister. Panoyu görmesi gereken roller (Eğitmen,
--    Koordinatör) rapor izni olmadan menüyü göremiyordu. Alt menüler değişmez (kpi:read,
--    kpi_snapshot:read); modül bayrağı (feature_flag = kpi) aynen kalır.
--
-- Menü ağacı Redis'te 1 saat önbelleklenir; güncellemeden sonra menü önbelleğini temizleyin
-- (KURULUM_KILAVUZU A.8). Yalnızca veri, idempotent.
-- =====================================================================
update iam.menu_item m
   set required_permission_id = p.id,
       updated_at             = now()
  from iam.permission p
 where m.corporation_id is null
   and (m.code, p.code) in (('educators', 'educator:read'), ('performance', 'kpi_dashboard:read'))
   and m.required_permission_id is distinct from p.id;
