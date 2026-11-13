namespace Aynesil.Shared.Constants;

/// <summary>JWT'de kullanılan özel claim adları.</summary>
public static class AuthClaims
{
    /// <summary>
    /// "true" ise hesap sahibi parolasını değiştirmeden yalnızca parola değiştirme / çıkış / token yenileme
    /// uçlarını kullanabilir (<c>PasswordChangeRequiredMiddleware</c>). iam.user_account.must_change_password'dan gelir.
    /// </summary>
    public const string MustChangePassword = "mcp";
}
