-- =============================================================================
-- AyNesil Platform :: Flyway V28 — Auth Function Fix (admin kullanıcı seed'i kaldırıldı)
-- =============================================================================
-- Düzeltmeler:
--   1. find_session_by_token — V5'in ip_address'i text'e çevirmesiyle oluşan
--      42P13 return-type mismatch hatası düzeltildi (inet → text).
--   (Eskiden 2. adım her 'admin' rolü olan kuruma admin/Admin123! kullanıcısı açıyordu;
--    henüz canlı kurulum yokken kaldırıldı. Kurum yöneticisi devops/new_tenant.py ile,
--    üretilmiş geçici şifre ve must_change_password = true ile açılır.)
--
-- Idempotent: OR REPLACE fonksiyon.
-- =============================================================================


-- ── Step 1: find_session_by_token — ip_address inet → text ───────────────────
-- V4 fonksiyonu ip_address inet olarak tanımladı.
-- V5 ALTER TABLE ile kolonu text'e çevirdi.
-- PostgreSQL'de CREATE OR REPLACE, return type değiştirmeye izin vermiyor (42P13).
-- Çözüm: Önce DROP, ardından yeni imzayla CREATE.

DROP FUNCTION IF EXISTS iam.find_session_by_token(text);

CREATE FUNCTION iam.find_session_by_token(p_token_hash text)
RETURNS TABLE(
    id                 uuid,
    corporation_id     uuid,
    user_id            uuid,
    issued_at          timestamptz,
    expires_at         timestamptz,
    revoked_at         timestamptz,
    refresh_token_hash text,
    ip_address         text,        -- V5 sonrası text (inet → text dönüşümü)
    user_agent         text
)
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = iam, public
AS $$
    SELECT id, corporation_id, user_id, issued_at, expires_at,
           revoked_at, refresh_token_hash, ip_address, user_agent
    FROM iam.auth_session
    WHERE refresh_token_hash = p_token_hash
      AND revoked_at IS NULL
      AND expires_at > now();
$$;

COMMENT ON FUNCTION iam.find_session_by_token(text) IS
    'SECURITY DEFINER: RLS bypass for refresh token validation. ip_address text (V5 fix).';

GRANT EXECUTE ON FUNCTION iam.find_session_by_token(text) TO aynesil_app;
