-- =============================================================================
-- AyNesil Platform :: V5 — Fix auth_session.ip_address type
-- =============================================================================
-- Sorun: EF Core AuthSession.IpAddress string? olarak map ediyor.
-- DDL'de inet olan kolon, EF Core tarafından text olarak gönderilince
-- PostgreSQL type mismatch hatası veriyor.
-- Çözüm: Kolonu text'e çevir — IP adresi string olarak saklanır.
-- Veri kaybı yok: inet::text cast idempotent.
-- =============================================================================

ALTER TABLE iam.auth_session
    ALTER COLUMN ip_address TYPE text USING ip_address::text;
