namespace Aynesil.Infrastructure.Options;

/// <summary>
/// JWT configuration bound from appsettings.json "Jwt" section.
/// Secret must be at least 32 characters and stored in a secret manager in production.
/// Never commit the secret to source control — use AYNESIL_Jwt__Secret env variable.
/// </summary>
public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Signing secret. Minimum 32 characters. Source from secret manager in production.</summary>
    public string Secret { get; set; } = string.Empty;

    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
    public string Algorithm { get; set; } = "HS256";
}
