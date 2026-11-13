-- =============================================================================
-- AyNesil Platform :: V50 — İlk girişte şifre değiştirme zorunluluğu
-- =============================================================================
-- Yönetici tarafından açılan hesaplarda (personel, eğitmen/veli hesabı, new_tenant.py yöneticisi)
-- parolayı yönetici belirler ya da geçici parola üretilir. Hesap sahibi ilk girişte bunu
-- değiştirmek zorundadır.
--
--   iam.user_account.must_change_password
--     true  → JWT'ye "mcp" claim'i konur; API yalnızca parola değiştirme / çıkış / token yenileme uçlarına
--             izin verir (PasswordChangeRequiredMiddleware), arayüz /account/password dışına bırakmaz.
--     false → normal oturum. Parola değiştirildiğinde (kendi isteğiyle ya da sıfırlama bağlantısıyla)
--             Domain'de UserAccount.ChangePassword bayrağı false yapar.
--
-- Mevcut hesaplar etkilenmez (varsayılan false).
-- iam.must_change_password(uuid): token üretimi sırasında tenant bağlamı yokken okumak için
-- SECURITY DEFINER (find_user_by_id ile aynı desen); yalnızca bayrağı döndürür.
-- =============================================================================

ALTER TABLE iam.user_account
    ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN iam.user_account.must_change_password IS
    'true: hesap sahibi parolasını değiştirene kadar yalnızca parola değiştirme/çıkış uçları kullanılabilir. Yönetici tarafından açılan hesaplarda true başlar.';

CREATE OR REPLACE FUNCTION iam.must_change_password(p_user_id uuid)
RETURNS boolean
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = iam, public
AS $$
    SELECT COALESCE(
        (SELECT must_change_password
           FROM iam.user_account
          WHERE id = p_user_id AND deleted_at IS NULL),
        false);
$$;

COMMENT ON FUNCTION iam.must_change_password(uuid) IS
    'SECURITY DEFINER: RLS bypass; token üretiminde "mcp" claim''ini belirlemek için yalnızca bayrağı döndürür.';

GRANT EXECUTE ON FUNCTION iam.must_change_password(uuid) TO aynesil_app;
