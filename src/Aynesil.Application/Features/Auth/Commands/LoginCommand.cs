using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Infrastructure.Persistence;
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
public class LoginCommandValidator : FluentValidation.AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly AynesilDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;

    public LoginCommandHandler(AynesilDbContext db, IPasswordService passwords, ITokenService tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    public async Task<LoginResult> Handle(LoginCommand req, CancellationToken ct)
    {
        // Username is case-insensitive (citext column in DB)
        var user = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Username == req.Username &&
                u.DeletedAt == null,
                ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.Status is "suspended" or "disabled")
            throw new UnauthorizedAccessException("Account is not active.");

        if (await _passwords.IsLockedOutAsync(user.Id, ct))
            throw new UnauthorizedAccessException("Account is temporarily locked. Please try again later.");

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !_passwords.Verify(req.Password, user.PasswordHash))
        {
            var locked = await _passwords.RecordFailedAttemptAsync(user.Id, ct);
            throw new UnauthorizedAccessException(locked
                ? "Account temporarily locked due to too many failed attempts."
                : "Invalid credentials.");
        }

        await _passwords.ResetFailedAttemptsAsync(user.Id, ct);

        // Collect permissions for the token
        var permissions = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id && ur.CorporationId == user.CorporationId)
            .Where(ur => ur.ValidFrom == null || ur.ValidFrom <= DateTimeOffset.UtcNow)
            .Where(ur => ur.ValidTo == null || ur.ValidTo >= DateTimeOffset.UtcNow)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync(ct);

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
}
