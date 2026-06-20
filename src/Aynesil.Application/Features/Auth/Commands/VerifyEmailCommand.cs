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
/// Verifies the email address of an invited user using the one-time token sent by RegisterCommand.
/// Transitions the user's status from 'invited' → 'active'.
/// </summary>
public record VerifyEmailCommand(string Token) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator() =>
        RuleFor(x => x.Token).NotEmpty();
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;

    public VerifyEmailCommandHandler(IAppDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task Handle(VerifyEmailCommand req, CancellationToken ct)
    {
        var tokenHash = Hash(req.Token);
        var cacheKey = CacheKeys.EmailVerificationToken(tokenHash);

        var tokenData = await _cache.GetAsync<TokenCacheData>(cacheKey, ct)
            ?? throw new UnauthorizedAccessException("Email verification token is invalid or has expired.");

        // Set tenant GUC so that EF/RLS permits the read and subsequent write
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_corporation_id', {0}, false), set_config('app.current_user_id', {1}, false)",
            tokenData.CorporationId.ToString(), tokenData.UserId.ToString());

        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == tokenData.UserId, ct)
            ?? throw new UnauthorizedAccessException("Email verification token is invalid or has expired.");

        if (user.Status != "invited")
        {
            // Already verified — remove stale token and return
            await _cache.RemoveAsync(cacheKey, ct);
            return;
        }

        user.Activate(updatedBy: null);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(cacheKey, ct);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
