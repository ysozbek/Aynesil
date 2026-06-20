using System.Security.Cryptography;
using System.Text;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared.Constants;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Auth.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

/// <summary>
/// Completes the password reset flow using the one-time token issued by RequestPasswordResetCommand.
/// Verifies the token from cache, sets the new password, and revokes all active sessions.
/// </summary>
public record ConfirmPasswordResetCommand(
    string Token,
    string NewPassword) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public class ConfirmPasswordResetCommandValidator : AbstractValidator<ConfirmPasswordResetCommand>
{
    public ConfirmPasswordResetCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ConfirmPasswordResetCommandHandler : IRequestHandler<ConfirmPasswordResetCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;
    private readonly ICacheService _cache;

    public ConfirmPasswordResetCommandHandler(
        IAppDbContext db,
        IPasswordService passwords,
        ITokenService tokens,
        ICacheService cache)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
        _cache = cache;
    }

    public async Task Handle(ConfirmPasswordResetCommand req, CancellationToken ct)
    {
        var tokenHash = Hash(req.Token);
        var cacheKey = CacheKeys.PasswordResetToken(tokenHash);

        var tokenData = await _cache.GetAsync<TokenCacheData>(cacheKey, ct)
            ?? throw new UnauthorizedAccessException("Password reset token is invalid or has expired.");

        // Set tenant GUC so that EF/RLS permits the read and subsequent write
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_corporation_id', {0}, false), set_config('app.current_user_id', {1}, false)",
            tokenData.CorporationId.ToString(), tokenData.UserId.ToString());

        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == tokenData.UserId, ct)
            ?? throw new UnauthorizedAccessException("Password reset token is invalid or has expired.");

        user.ChangePassword(_passwords.Hash(req.NewPassword), updatedBy: null);
        await _db.SaveChangesAsync(ct);

        await _tokens.RevokeAllAsync(tokenData.UserId, ct);
        await _cache.RemoveAsync(cacheKey, ct);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
