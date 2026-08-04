-- =============================================================================
-- AyNesil Platform :: Flyway V29 — Fix ip_address column types (inet → text)
-- =============================================================================
-- Sorun: EF Core, ip_address kolonlarını string? (text) olarak map eder.
-- V5 yalnızca iam.auth_session.ip_address'i düzeltti.
-- core.activity_log ve media.viewing_log'da aynı uyumsuzluk devam ediyordu.
--
-- Etkilenen tablolar:
--   core.activity_log.ip_address  — ActivityLoggingMiddleware INSERT hataları
--   media.viewing_log.ip_address  — ViewingLog entity INSERT hataları
--
-- PostgreSQL 14+: ALTER TABLE ... ALTER COLUMN TYPE partitioned tabloda
-- tüm alt partition'lara cascade eder.
-- Idempotent: kolon zaten text ise USING cast değişiklik yapmaz.
-- =============================================================================

-- ── core.activity_log ────────────────────────────────────────────────────────

ALTER TABLE core.activity_log
    ALTER COLUMN ip_address TYPE text USING ip_address::text;

-- ── media.viewing_log ────────────────────────────────────────────────────────

ALTER TABLE media.viewing_log
    ALTER COLUMN ip_address TYPE text USING ip_address::text;
