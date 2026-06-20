using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    Guid CorporationId);

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

    public LoginCommandHandler(IAppDbContext db, IPasswordService passwords, ITokenService tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
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
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        // ── Step 2: Status check ───────────────────────────────────────────────
        if (user.Status is "suspended" or "disabled")
            throw new UnauthorizedAccessException("Account is not active.");

        // ── Step 3: Lockout check ─────────────────────────────────────────────
        if (await _passwords.IsLockedOutAsync(user.Id, ct))
            throw new UnauthorizedAccessException("Account is temporarily locked. Please try again later.");

        // ── Step 4: Password verification ─────────────────────────────────────
        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !_passwords.Verify(req.Password, user.PasswordHash))
        {
            var locked = await _passwords.RecordFailedAttemptAsync(user.Id, ct);
            throw new UnauthorizedAccessException(locked
                ? "Account temporarily locked due to too many failed attempts."
                : "Invalid credentials.");
        }

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

        return new LoginResult(
            tokenPair.AccessToken,
            tokenPair.RefreshToken,
            tokenPair.ExpiresAt,
            user.FullName,
            user.Email,
            user.PreferredLocale,
            user.Id,
            user.CorporationId);
    }

    private record PermissionRow(string PermissionCode);
}
