-- =============================================================================
-- AyNesil Platform :: Flyway V30 — Fix ref.ref_value audit columns
-- =============================================================================
-- Sorun: EF Core RefValue entity'si BaseEntity'den inherit ederek
-- deleted_by, created_by, updated_by kolonlarını map ediyor.
-- ref.ref_value tablosu V1'de bu kolonlar olmadan oluşturulmuş.
-- Sonuç: Herhangi bir ref_value sorgusu "column r.deleted_by does not exist" hatası veriyor.
--
-- Çözüm: Eksik audit kolonlarını ekle (NULL izin ver — mevcut satırlar etkilenmesin).
-- Idempotent: IF NOT EXISTS.
-- =============================================================================

ALTER TABLE ref.ref_value
    ADD COLUMN IF NOT EXISTS created_by uuid,
    ADD COLUMN IF NOT EXISTS updated_by uuid,
    ADD COLUMN IF NOT EXISTS deleted_by uuid;
