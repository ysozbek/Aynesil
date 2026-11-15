using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aynesil.Application.Features.Auth.Commands;

// ── Request ─────────────────────────────────────────────────────────────────
public record LoginCommand(
    string Username,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<LoginResult>;

public record LoginResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string FullName,
    string? Email,
    string? Locale,
    Guid UserId,
    Guid CorporationId,
    bool MustChangePassword = false);

// ── Validator ────────────────────────────────────────────────────────────────
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

// ── Auth result DTO (maps from iam.authenticate_user() function result) ──────
internal record AuthUserRow(
    Guid Id,
    Guid CorporationId,
    string Username,
    string? Email,
    string FullName,
    string? PasswordHash,
    string Status,
    string? PreferredLocale);

// ── Handler ──────────────────────────────────────────────────────────────────
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAppDbContext db, IPasswordService passwords, ITokenService tokens, ILogger<LoginCommandHandler> logger)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(LoginCommand req, CancellationToken ct)
    {
        // ── Step 1: Lookup user via SECURITY DEFINER function (bypasses RLS) ──
        // During login, tenant context (app.current_corporation_id) is not yet set.
        // Direct table query would return 0 rows due to default-deny RLS.
        // iam.authenticate_user() runs as the DB owner and bypasses RLS safely.
        // EF Core subquery'de PascalCase column adı arar — alias ile eşleştir
        var user = await _db.Database
            .SqlQueryRaw<AuthUserRow>(
                @"SELECT id               AS ""Id"",
                         corporation_id   AS ""CorporationId"",
                         username         AS ""Username"",
                         email            AS ""Email"",
                         full_name        AS ""FullName"",
                         password_hash    AS ""PasswordHash"",
                         status           AS ""Status"",
                         preferred_locale AS ""PreferredLocale""
                  FROM iam.authenticate_user({0}::citext)",
                req.Username)
            .FirstOrDefaultAsync(ct)
            ?? await FailUnknownUserAsync(req.Password, req.IpAddress);

        // ── Step 2: Password verification FIRST ────────────────────────────────
        // Hesap durumu (kilitli/devre dışı) yalnızca parola doğruysa bildirilir; aksi halde her başarısızlık
        // aynı "geçersiz kimlik bilgisi" yanıtını verir ve kullanıcının var olup olmadığı sızmaz.
        var inactive = user.Status is "suspended" or "disabled";
        var lockedOut = await _passwords.IsLockedOutAsync(user.Id, ct);
        var passwordOk = _passwords.VerifyOrDummy(req.Password, user.PasswordHash);

        if (!passwordOk)
        {
            // Kilitli/devre dışı hesaplarda deneme sayacı artırılmaz (kilit süresi uzamasın).
            if (!inactive && !lockedOut)
                await _passwords.RecordFailedAttemptAsync(user.Id, ct);
            _logger.LogWarning("Login failed for user {UserId} from {ClientIp}: invalid password", user.Id, req.IpAddress);
            throw new AuthenticationFailedException(MsgKeys.Auth.InvalidCredentials);
        }

        // ── Step 3: Account state (password is correct, so revealing it is safe) ─
        if (inactive)
            throw Rejected(user, req.IpAddress, MsgKeys.Auth.AccountDisabled);
        if (lockedOut)
            throw Rejected(user, req.IpAddress, MsgKeys.Auth.AccountLocked);
        // Askıya alınmış/kapatılmış kurumun kullanıcısı giremez (V57; kurum satırı RLS'li, fonksiyon SECURITY DEFINER).
        if (!await CorporationIsActiveAsync(user.CorporationId, ct))
            throw Rejected(user, req.IpAddress, MsgKeys.Auth.CorporationInactive);

        await _passwords.ResetFailedAttemptsAsync(user.Id, ct);

        // ── Step 5: Load permissions via SECURITY DEFINER function ─────────────
        var permissions = await _db.Database
            .SqlQueryRaw<PermissionRow>(
                @"SELECT permission_code AS ""PermissionCode"" FROM iam.get_user_permissions({0}, {1})",
                user.Id, user.CorporationId)
            .Select(r => r.PermissionCode)
            .ToListAsync(ct);

        // ── Step 6: Issue JWT + refresh token ─────────────────────────────────
        var tokenPair = await _tokens.IssueTokensAsync(
            user.Id, user.CorporationId, permissions,
            req.IpAddress, req.UserAgent, ct);

        _logger.LogInformation("Login succeeded for user {UserId} (corporation {CorporationId}) from {ClientIp}",
            user.Id, user.CorporationId, req.IpAddress);

        return new LoginResult(
            tokenPair.AccessToken,
            tokenPair.RefreshToken,
            tokenPair.ExpiresAt,
            user.FullName,
            user.Email,
            user.PreferredLocale,
            user.Id,
            user.CorporationId,
            tokenPair.MustChangePassword);
    }

    /// <summary>
    /// Bilinmeyen kullanıcıda da gerçek bir BCrypt doğrulaması çalışır; yanıt süresi "parola yanlış" ile aynı olur.
    /// </summary>
    private Task<AuthUserRow> FailUnknownUserAsync(string password, string? clientIp)
    {
        _passwords.VerifyOrDummy(password, null);
        // Kullanıcı adı loglanmaz (yanlış alana yazılmış bir parola olabilir); yalnızca kaynak IP.
        _logger.LogWarning("Login failed for an unknown username from {ClientIp}", clientIp);
        throw new AuthenticationFailedException(MsgKeys.Auth.InvalidCredentials);
    }

    private Task<bool> CorporationIsActiveAsync(Guid corporationId, CancellationToken ct) =>
        _db.Database
            .SqlQueryRaw<bool>(@"SELECT iam.corporation_is_active({0}) AS ""Value""", corporationId)
            .FirstAsync(ct);

    private AuthenticationFailedException Rejected(AuthUserRow user, string? clientIp, MsgKey reason)
    {
        _logger.LogWarning("Login rejected for user {UserId} from {ClientIp}: {Reason}", user.Id, clientIp, reason.Value);
        return new AuthenticationFailedException(reason);
    }

    private record PermissionRow(string PermissionCode);
}
