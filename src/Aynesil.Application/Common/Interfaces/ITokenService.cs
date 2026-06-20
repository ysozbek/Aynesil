namespace Aynesil.Application.Common.Interfaces;

public record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

/// <summary>
/// Generates and validates JWT access tokens and opaque refresh tokens.
/// Access tokens are short-lived (60min default) and signed with HS256.
/// Refresh tokens are cryptographically random, hashed before storage (SHA-256).
/// Refresh token rotation: each use issues a new token and revokes the previous session.
/// </summary>
public interface ITokenService
{
    /// <summary>Issues a new access + refresh token pair and creates an AuthSession record.</summary>
    Task<TokenPair> IssueTokensAsync(
        Guid userId,
        Guid corporationId,
        IEnumerable<string> permissionCodes,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>Validates a refresh token, revokes the current session, issues a new pair.</summary>
    Task<TokenPair> RefreshAsync(
        string refreshToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes the session associated with the given refresh token.</summary>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes all sessions for a user (e.g. on password change).</summary>
    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default);
}
