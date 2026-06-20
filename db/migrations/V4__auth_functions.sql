-- =============================================================================
-- AyNesil Platform :: V4 — Authentication Helper Functions
-- =============================================================================
-- Sorun: Login sırasında tenant context (app.current_corporation_id) henüz yok.
-- RLS default-deny olduğu için iam.user_account sorgusu 0 satır döndürür.
-- Çözüm: SECURITY DEFINER fonksiyonlar — fonksiyon sahibi (aynesil_owner)
-- olarak çalışır, RLS bypass eder. Sadece auth için gerekli alanlar açığa çıkar.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- iam.authenticate_user — Kullanıcıyı username ile sorgula (RLS bypass)
-- Güvenlik: Sadece aktif, silinmemiş kullanıcıları döndürür.
-- password_hash dahil — uygulama katmanı BCrypt ile doğrular.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION iam.authenticate_user(p_username citext)
RETURNS TABLE(
    id              uuid,
    corporation_id  uuid,
    username        citext,
    email           citext,
    full_name       text,
    password_hash   text,
    status          text,
    preferred_locale text
)
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = iam, core, public
AS $$
    SELECT
        id, corporation_id, username, email, full_name,
        password_hash, status, preferred_locale
    FROM iam.user_account
    WHERE username = p_username
      AND deleted_at IS NULL;
$$;

COMMENT ON FUNCTION iam.authenticate_user(citext) IS
    'SECURITY DEFINER: RLS bypass for initial authentication. Returns only fields needed for login verification.';

GRANT EXECUTE ON FUNCTION iam.authenticate_user(citext) TO aynesil_app;

-- -----------------------------------------------------------------------------
-- iam.get_user_permissions — Kullanıcının permission code'larını getir (RLS bypass)
-- Login sonrası JWT token'a eklenecek permission listesi.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION iam.get_user_permissions(p_user_id uuid, p_corp_id uuid)
RETURNS TABLE(permission_code text)
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = iam, public
AS $$
    SELECT DISTINCT p.code
    FROM iam.user_role ur
    JOIN iam.role r          ON r.id  = ur.role_id
    JOIN iam.role_permission rp ON rp.role_id = r.id
    JOIN iam.permission p    ON p.id  = rp.permission_id
    WHERE ur.user_id        = p_user_id
      AND ur.corporation_id = p_corp_id
      AND (ur.valid_from IS NULL OR ur.valid_from <= now())
      AND (ur.valid_to   IS NULL OR ur.valid_to   >= now());
$$;

COMMENT ON FUNCTION iam.get_user_permissions(uuid, uuid) IS
    'SECURITY DEFINER: RLS bypass to load permission codes for JWT after successful authentication.';

GRANT EXECUTE ON FUNCTION iam.get_user_permissions(uuid, uuid) TO aynesil_app;

-- -----------------------------------------------------------------------------
-- iam.find_session_by_token — Refresh token ile session bul (RLS bypass)
-- Refresh sırasında henüz tenant context yok.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION iam.find_session_by_token(p_token_hash text)
RETURNS TABLE(
    id                 uuid,
    corporation_id     uuid,
    user_id            uuid,
    issued_at          timestamptz,
    expires_at         timestamptz,
    revoked_at         timestamptz,
    refresh_token_hash text,
    ip_address         inet,
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
    'SECURITY DEFINER: RLS bypass for refresh token validation.';

GRANT EXECUTE ON FUNCTION iam.find_session_by_token(text) TO aynesil_app;

-- -----------------------------------------------------------------------------
-- iam.find_user_by_id — ID ile kullanıcı bul (RLS bypass)
-- IssueTokensAsync sırasında user bilgisi için gerekli.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION iam.find_user_by_id(p_user_id uuid)
RETURNS TABLE(
    id               uuid,
    corporation_id   uuid,
    full_name        text,
    email            citext,
    preferred_locale text
)
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = iam, public
AS $$
    SELECT id, corporation_id, full_name, email, preferred_locale
    FROM iam.user_account
    WHERE id = p_user_id
      AND deleted_at IS NULL;
$$;

GRANT EXECUTE ON FUNCTION iam.find_user_by_id(uuid) TO aynesil_app;
