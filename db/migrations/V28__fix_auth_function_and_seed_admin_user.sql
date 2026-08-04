-- =============================================================================
-- AyNesil Platform :: Flyway V28 — Auth Function Fix + Admin User Seed
-- =============================================================================
-- Düzeltmeler:
--   1. find_session_by_token — V5'in ip_address'i text'e çevirmesiyle oluşan
--      42P13 return-type mismatch hatası düzeltildi (inet → text).
--   2. Admin kullanıcısı ve user_role ataması seed edildi (geliştirme ortamı).
--
-- Varsayılan giriş bilgileri (DEVELOPMENT ONLY):
--   Kullanıcı adı : admin
--   Şifre         : Admin123!
--
-- Idempotent: ON CONFLICT DO NOTHING / OR REPLACE fonksiyon.
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


-- ── Step 2: Admin kullanıcısı seed (geliştirme ortamı) ───────────────────────
-- 'admin' rolü olan HER kurum için bir admin kullanıcısı oluşturulur.
-- Şifre: "Admin123!" — bcrypt cost 12 (pgcrypto gen_salt bf).
-- Sadece geliştirme/demo ortamı için. Production'da farklı şifre kullanın.

INSERT INTO iam.user_account
    (corporation_id, username, full_name, password_hash, status, preferred_locale)
SELECT
    r.corporation_id,
    'admin',
    'Sistem Yöneticisi',
    crypt('Admin123!', gen_salt('bf', 12)),
    'active',
    'tr'
FROM iam.role r
WHERE r.code = 'admin'
  AND r.corporation_id IS NOT NULL
ON CONFLICT (corporation_id, username) DO NOTHING;


-- ── Step 3: Admin kullanıcısına admin rolü ata ────────────────────────────────

INSERT INTO iam.user_role (corporation_id, user_id, role_id, campus_id, valid_from, valid_to)
SELECT
    u.corporation_id,
    u.id,
    r.id,
    NULL,
    NULL,
    NULL
FROM iam.user_account u
JOIN iam.role r ON r.corporation_id = u.corporation_id AND r.code = 'admin'
WHERE u.username = 'admin'
ON CONFLICT (user_id, role_id, campus_id) DO NOTHING;
