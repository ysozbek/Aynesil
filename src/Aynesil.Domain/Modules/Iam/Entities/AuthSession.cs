namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.auth_session.
/// Tracks refresh token sessions. A session is valid until ExpiresAt unless revoked.
/// Refresh token rotation: each refresh creates a new session and revokes the previous one.
/// RefreshTokenHash stores the bcrypt/sha256 hash of the token — never the raw token.
/// </summary>
public class AuthSession : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid UserId { get; set; }

    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive => RevokedAt == null && DateTimeOffset.UtcNow < ExpiresAt;

    /// <summary>SHA-256 hash of the refresh token. Never stores the raw token.</summary>
    public string? RefreshTokenHash { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public UserAccount? User { get; set; }

    public void Revoke() => RevokedAt = DateTimeOffset.UtcNow;
}
