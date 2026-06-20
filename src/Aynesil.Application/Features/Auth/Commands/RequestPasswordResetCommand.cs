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
/// Initiates the password reset flow for a registered user.
/// Looks up the user by email (bypassing RLS via SECURITY DEFINER function),
/// generates a one-time token, stores its hash in Redis, and sends a notification.
/// Always returns success — never reveals whether the email is registered (OWASP).
/// </summary>
public record RequestPasswordResetCommand(string Email) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator() =>
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly INotificationService _notifications;

    public RequestPasswordResetCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        INotificationService notifications)
    {
        _db = db;
        _cache = cache;
        _notifications = notifications;
    }

    public async Task Handle(RequestPasswordResetCommand req, CancellationToken ct)
    {
        // Use SECURITY DEFINER function — no RLS tenant context during unauthenticated reset flow
        var user = await _db.Database
            .SqlQueryRaw<UserEmailRow>(
                @"SELECT id               AS ""Id"",
                         corporation_id   AS ""CorporationId"",
                         email            AS ""Email"",
                         status           AS ""Status""
                  FROM iam.find_user_by_email({0}::citext)",
                req.Email.ToLowerInvariant())
            .FirstOrDefaultAsync(ct);

        // OWASP: do not reveal whether email exists — return silently if not found
        if (user is null || user.Status is "disabled" or "suspended")
            return;

        var (rawToken, tokenHash) = GenerateToken();

        await _cache.SetAsync(
            CacheKeys.PasswordResetToken(tokenHash),
            new TokenCacheData(user.Id, user.CorporationId),
            expiry: TimeSpan.FromHours(1),
            cancellationToken: ct);

        await _notifications.SendAsync(new SendNotificationRequest(
            RecipientUserId: user.Id,
            TemplateCode: "user.password_reset",
            Variables: new Dictionary<string, string> { ["token"] = rawToken }), ct);
    }

    private static (string Raw, string Hash) GenerateToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var raw = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return (raw, hash);
    }

    private record UserEmailRow(Guid Id, Guid CorporationId, string? Email, string Status);
}
