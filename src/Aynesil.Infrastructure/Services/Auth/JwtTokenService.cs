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
/// RLS notu: Bu servis authentication akışında kullanılır — JWT henüz yok,
/// tenant context (app.current_corporation_id GUC) henüz set edilmemiş.
/// Doğrudan tablo sorgusu yerine SECURITY DEFINER DB fonksiyonları kullanılır:
///   iam.find_user_by_id(uuid)          → user bilgisi
///   iam.find_session_by_token(text)    → refresh token doğrulama
///   iam.get_user_permissions(uuid,uuid)→ permission listesi
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
        // Kullanıcı bilgisini SECURITY DEFINER fonksiyon ile getir (RLS bypass)
        var userRow = await _db.Database
            .SqlQueryRaw<UserInfoRow>(
                @"SELECT id               AS ""Id"",
                         corporation_id   AS ""CorporationId"",
                         full_name        AS ""FullName"",
                         email            AS ""Email"",
                         preferred_locale AS ""PreferredLocale""
                  FROM iam.find_user_by_id({0})",
                userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var (rawRefresh, hashRefresh) = GenerateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_opts.RefreshTokenExpiryDays);

        // EnableRetryOnFailure + user transaction: CreateExecutionStrategy() wrapper gerekir.
        // Transaction içinde SET GUC + INSERT aynı connection'da kalır → RLS WITH CHECK geçer.
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            await SetTenantGucAsync(corporationId, userId, cancellationToken);

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
            await tx.CommitAsync(cancellationToken);
        });

        var accessToken = BuildAccessToken(userRow, corporationId, permissionCodes);
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

        // Session'ı SECURITY DEFINER fonksiyon ile bul (RLS bypass — corp context yok henüz)
        var session = await _db.Database
            .SqlQueryRaw<SessionRow>(
                @"SELECT id                              AS ""Id"",
                         corporation_id                  AS ""CorporationId"",
                         user_id                         AS ""UserId"",
                         issued_at                       AS ""IssuedAt"",
                         expires_at                      AS ""ExpiresAt"",
                         revoked_at                      AS ""RevokedAt"",
                         refresh_token_hash              AS ""RefreshTokenHash"",
                         ip_address::text                AS ""IpAddress"",
                         user_agent                      AS ""UserAgent""
                  FROM iam.find_session_by_token({0})",
                tokenHash)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        // Session'ı revoke et (rotation)
        await SetTenantGucAsync(session.CorporationId, session.UserId, cancellationToken);

        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE iam.auth_session SET revoked_at = now() WHERE id = {0}",
            session.Id);

        // Permissions yükle ve yeni token çıkar
        var permissions = await _db.Database
            .SqlQueryRaw<PermissionRow>(
                @"SELECT permission_code AS ""PermissionCode"" FROM iam.get_user_permissions({0}, {1})",
                session.UserId, session.CorporationId)
            .Select(r => r.PermissionCode)
            .ToListAsync(cancellationToken);

        return await IssueTokensAsync(session.UserId, session.CorporationId, permissions, ipAddress, userAgent, cancellationToken);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(refreshToken);
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE iam.auth_session SET revoked_at = now() WHERE refresh_token_hash = {0} AND revoked_at IS NULL",
            tokenHash);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE iam.auth_session SET revoked_at = now() WHERE user_id = {0} AND revoked_at IS NULL",
            userId);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task SetTenantGucAsync(Guid corporationId, Guid userId, CancellationToken ct)
    {
        // RLS with-check ve audit_trigger için GUC'u set et
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_corporation_id', {0}, false), set_config('app.current_user_id', {1}, false)",
            corporationId.ToString(), userId.ToString());
    }

    private string BuildAccessToken(UserInfoRow user, Guid corporationId, IEnumerable<string> permissions)
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
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ── Private DTO records (EF Core raw SQL mapping) ─────────────────────────
    private record UserInfoRow(Guid Id, Guid CorporationId, string FullName, string? Email, string? PreferredLocale);
    private record SessionRow(Guid Id, Guid CorporationId, Guid UserId, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt, string? RefreshTokenHash, string? IpAddress, string? UserAgent);
    private record PermissionRow(string PermissionCode);
}
