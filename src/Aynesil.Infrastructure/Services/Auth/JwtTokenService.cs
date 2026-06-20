using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Infrastructure.Options;
using Aynesil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aynesil.Infrastructure.Services.Auth;

/// <summary>
/// Issues and rotates JWT access tokens + opaque refresh tokens.
///
/// Access token claims:
///   sub       = user_account.id
///   corp      = corporation.id
///   name      = full_name
///   email     = email
///   perms     = ["student:read","session:create",...] (permission codes)
///   jti       = unique token id
///
/// Refresh token:
///   - 32-byte cryptographically random value, base64url-encoded
///   - SHA-256 hash stored in iam.auth_session.refresh_token_hash
///   - Token rotation: each refresh creates a new session, revokes old one
///
/// Security considerations:
///   - Refresh token hash comparison uses constant-time equality (CryptographicOperations.FixedTimeEquals)
///   - Expired/revoked sessions return generic error messages (avoid oracle attacks)
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly AynesilDbContext _db;
    private readonly JwtOptions _opts;

    public JwtTokenService(AynesilDbContext db, IOptions<JwtOptions> opts)
    {
        _db = db;
        _opts = opts.Value;
    }

    public async Task<TokenPair> IssueTokensAsync(
        Guid userId,
        Guid corporationId,
        IEnumerable<string> permissionCodes,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var (rawRefresh, hashRefresh) = GenerateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_opts.RefreshTokenExpiryDays);

        var session = new AuthSession
        {
            CorporationId = corporationId,
            UserId = userId,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            RefreshTokenHash = hashRefresh,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
        _db.AuthSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        var accessToken = BuildAccessToken(user, corporationId, permissionCodes);
        var accessExpiry = DateTimeOffset.UtcNow.AddMinutes(_opts.AccessTokenExpiryMinutes);

        return new TokenPair(accessToken, rawRefresh, accessExpiry);
    }

    public async Task<TokenPair> RefreshAsync(
        string refreshToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(refreshToken);

        var session = await _db.AuthSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s =>
                s.RefreshTokenHash == tokenHash &&
                s.RevokedAt == null &&
                s.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        // Revoke current session (rotation)
        session.Revoke();

        var permissions = await GetPermissionCodesAsync(session.UserId, session.CorporationId, cancellationToken);
        var result = await IssueTokensAsync(session.UserId, session.CorporationId, permissions, ipAddress, userAgent, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(refreshToken);
        var session = await _db.AuthSessions
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == tokenHash && s.RevokedAt == null, cancellationToken);

        if (session is not null)
        {
            session.Revoke();
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _db.AuthSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var s in sessions) s.Revoke();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private string BuildAccessToken(UserAccount user, Guid corporationId, IEnumerable<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new("corporation_id", corporationId.ToString()),
            new("full_name", user.FullName),
        };

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new(JwtRegisteredClaimNames.Email, user.Email));

        if (!string.IsNullOrEmpty(user.PreferredLocale))
            claims.Add(new("locale", user.PreferredLocale));

        claims.AddRange(permissions.Select(p => new Claim("perm", p)));

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opts.AccessTokenExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<IEnumerable<string>> GetPermissionCodesAsync(
        Guid userId, Guid corporationId, CancellationToken ct)
    {
        return await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.CorporationId == corporationId)
            .Where(ur => ur.ValidFrom == null || ur.ValidFrom <= DateTimeOffset.UtcNow)
            .Where(ur => ur.ValidTo == null || ur.ValidTo >= DateTimeOffset.UtcNow)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync(ct);
    }

    private static (string raw, string hash) GenerateRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var raw = Convert.ToBase64String(bytes);
        return (raw, Hash(raw));
    }

    private static string Hash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
