-- =============================================================================
-- AyNesil Platform :: Flyway V37 — Eski performance_kpi menü grubunu kapat
-- =============================================================================
-- Sorun:
--   V23, '/performance-kpi/...' adreslerine giden bir menü grubu ekledi
--   (KPI Tanımları, Eğitmen Paneli, Yönetici Paneli, Yönetim Paneli,
--   Performans Raporları). Vue Router'da bu adreslerin hiçbiri yok; tıklanınca
--   404'e düşüyor. V27 yalnızca üst kaydı kapattı, alt kayıtlar açık kaldı ve
--   menüde görünmeye devam ediyor.
--
-- Doğru karşılıklar zaten V8/V27 'performance' grubunda:
--   KPI & Performans (/kpi)  → Yönetici / Yönetim / Sıralama sekmeleri
--   KPI Tanımları (/kpi/definitions), Performans Anlık Görüntüleri (/kpi/snapshots)
--   Eğitmen Paneli ve Performans Raporları için henüz sayfa yok.
--
-- Değişiklik: performance_kpi ve tüm alt kayıtları (platform varsayılanı ve
-- kurum kopyaları) is_active = false. Kayıtlar silinmez.
-- Idempotent.
-- =============================================================================

WITH RECURSIVE legacy AS (
    SELECT id FROM iam.menu_item WHERE code = 'performance_kpi'
    UNION
    SELECT m.id FROM iam.menu_item m JOIN legacy l ON m.parent_id = l.id
)
UPDATE iam.menu_item
   SET is_active = false
 WHERE (id IN (SELECT id FROM legacy) OR route LIKE '/performance-kpi%')
   AND is_active;
