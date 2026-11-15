-- =============================================================================
-- AyNesil Platform :: Flyway V57 — Pasif kurumda giriş ve token yenileme reddi
-- =============================================================================
-- Sorun: giriş (iam.authenticate_user) ve token yenileme (iam.find_session_by_token)
--   yalnızca kullanıcının durumuna bakıyordu; status <> 'active' (suspended/closed)
--   kurumun kullanıcıları giriş yapabiliyor ve oturumlarını yenileyebiliyordu.
--
-- Çözüm: iam.corporation_is_active(uuid) — SECURITY DEFINER. Giriş ve yenileme
--   anında henüz kurum bağlamı (app.current_corporation_id) yoktur ve core.corporation
--   V43'ten beri RLS'lidir; bu yüzden diğer auth fonksiyonları gibi sahip rolüyle çalışır.
--   Yalnızca boolean döner (kurum adı/ayarları sızmaz). Silinmiş kurum etkin sayılmaz.
--   API: LoginCommandHandler, JwtTokenService.RefreshAsync; oturum içi istekler
--   StaleTenantMiddleware'de (kurum satırı RLS ile kendi bağlamında okunur).
--
-- Idempotent: CREATE OR REPLACE.
-- =============================================================================

CREATE OR REPLACE FUNCTION iam.corporation_is_active(p_corporation_id uuid)
RETURNS boolean
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = core, public
AS $$
    SELECT EXISTS (
        SELECT 1
          FROM core.corporation
         WHERE id = p_corporation_id
           AND status = 'active'
           AND deleted_at IS NULL
    );
$$;

COMMENT ON FUNCTION iam.corporation_is_active(uuid) IS
    'SECURITY DEFINER: login/refresh check that the tenant is active (V57). Returns only a boolean.';

REVOKE ALL ON FUNCTION iam.corporation_is_active(uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION iam.corporation_is_active(uuid) TO aynesil_app;
