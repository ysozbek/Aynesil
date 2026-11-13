-- =============================================================================
-- AyNesil Platform :: V51 — Yeni ekranların menü kayıtları
-- =============================================================================
-- Ekransız uç noktaları kapatan işlerle iki menü öğesi yeniden kullanılabilir oldu:
--   1. payments-refunds   → /finance/refunds (İade Talepleri). V14'te /payments/refunds olarak tohumlandı,
--                           V31 ekran olmadığı için pasifleştirmişti.
--   2. legal.consent_templates → /legal/consent-templates (Onam Şablonları). V31 ekran olmadığı için
--                           pasifleştirmişti; rota artık var (TemplateListView).
-- Yalnızca platform varsayılanları (corporation_id IS NULL) güncellenir. Idempotent.
-- =============================================================================

UPDATE iam.menu_item
   SET route = '/finance/refunds', is_active = true
 WHERE code = 'payments-refunds' AND corporation_id IS NULL;

UPDATE iam.menu_item
   SET is_active = true
 WHERE code = 'legal.consent_templates' AND corporation_id IS NULL;

-- Etiketler (tr/en) V14 ve V25'te zaten tohumlandı; değişmez.
