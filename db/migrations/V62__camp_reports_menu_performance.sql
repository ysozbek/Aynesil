-- =====================================================================
-- V62 — Kamp menüsü: "Kamp Raporları" → /camps/performance, "Kamp Devam Takibi" pasif
--
-- /camps/performance (kamp performans raporu) eklendi; sayfa ve ucu camp:read iznine bağlı.
-- "Kamp Raporları" satırı artık bu sayfaya gider ve camp:read ile görünür.
-- Yoklama ayrı bir ekran değil, dönem detayındaki sekmedir (/camps/periods/:id); bu yüzden
-- "Kamp Devam Takibi" satırı pasif kalır (V31'de pasifleştirilmişti; burada açıkça garantilenir).
-- Şema değişikliği yok; yalnızca veri. Idempotent.
-- =====================================================================

update iam.menu_item m
   set route = '/camps/performance',
       required_permission_id = (select p.id from iam.permission p where p.code = 'camp:read'),
       is_active = true
 where m.code = 'camps.reports' and m.corporation_id is null;

update iam.menu_item
   set is_active = false
 where code = 'camps.attendance' and corporation_id is null;
