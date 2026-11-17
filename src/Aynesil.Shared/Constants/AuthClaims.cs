namespace Aynesil.Shared.Constants;

/// <summary>JWT'de kullanılan özel claim adları.</summary>
public static class AuthClaims
{
    /// <summary>
    /// "true" ise hesap sahibi parolasını değiştirmeden yalnızca parola değiştirme / çıkış / token yenileme
    /// uçlarını kullanabilir (<c>PasswordChangeRequiredMiddleware</c>). iam.user_account.must_change_password'dan gelir.
    /// </summary>
    public const string MustChangePassword = "mcp";

    /// <summary>
    /// Jetonun verildiği an, Unix milisaniye (V63). Standart <c>iat</c> saniye çözünürlüğündedir; iptalle aynı saniyede
    /// verilmiş eski jetonu yakalamak ve iptalden hemen sonraki yeni girişi reddetmemek için
    /// <c>UserSessionMiddleware</c> bunu <c>iam.user_account.sessions_revoked_at</c> ile karşılaştırır.
    /// </summary>
    public const string IssuedAtMs = "iat_ms";
}
