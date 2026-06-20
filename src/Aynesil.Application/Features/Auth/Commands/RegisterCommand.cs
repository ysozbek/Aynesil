using System.Security.Cryptography;
using System.Text;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Users.Dtos;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Shared.Constants;
using FluentValidation;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Auth.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

/// <summary>
/// Creates a new user account within the authenticated admin's corporation.
/// If an email address is provided and SendVerificationEmail is true, the account is created
/// with status='invited' and a verification email is dispatched via INotificationService.
/// If no email is provided, the account is activated immediately (status='active').
/// </summary>
public record RegisterCommand(
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    string? Password,
    string? PreferredLocale = null,
    Guid? PrimaryCampusId = null,
    bool SendVerificationEmail = true) : IRequest<UserDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z0-9][a-z0-9._-]*$")
            .WithMessage("Username must start with a letter or digit and contain only lowercase letters, digits, dots, hyphens, or underscores.");

        RuleFor(x => x.FullName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => x.Email is not null);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(200).When(x => x.Password is not null);
        RuleFor(x => x.PreferredLocale).MaximumLength(20).When(x => x.PreferredLocale is not null);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cache;
    private readonly INotificationService _notifications;

    public RegisterCommandHandler(
        IAppDbContext db,
        IPasswordService passwords,
        ICurrentUserService currentUser,
        ITenantContext tenantContext,
        ICacheService cache,
        INotificationService notifications)
    {
        _db = db;
        _passwords = passwords;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _cache = cache;
        _notifications = notifications;
    }

    public async Task<UserDto> Handle(RegisterCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required to register a user.");

        var usernameLower = req.Username.ToLowerInvariant();

        var taken = await _db.UserAccounts.AnyAsync(
            u => u.CorporationId == corporationId && u.Username == usernameLower, ct);
        if (taken)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.Username), $"Username '{usernameLower}' is already taken.")]);

        if (req.Email is not null)
        {
            var emailLower = req.Email.ToLowerInvariant();
            var emailTaken = await _db.UserAccounts.AnyAsync(
                u => u.CorporationId == corporationId && u.Email == emailLower, ct);
            if (emailTaken)
                throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                    nameof(req.Email), $"Email '{emailLower}' is already registered.")]);
        }

        var passwordHash = req.Password is not null ? _passwords.Hash(req.Password) : null;
        var sendVerification = req.Email is not null && req.SendVerificationEmail;

        var user = UserAccount.Create(
            corporationId,
            usernameLower,
            req.FullName,
            req.Email,
            req.Phone,
            passwordHash,
            req.PreferredLocale,
            req.PrimaryCampusId,
            createdBy: _currentUser.UserId,
            activateImmediately: !sendVerification);

        _db.UserAccounts.Add(user);
        await _db.SaveChangesAsync(ct);

        if (sendVerification)
        {
            var (rawToken, tokenHash) = GenerateToken();
            var cacheData = new TokenCacheData(user.Id, corporationId);

            await _cache.SetAsync(
                CacheKeys.EmailVerificationToken(tokenHash),
                cacheData,
                expiry: TimeSpan.FromHours(24),
                cancellationToken: ct);

            await _notifications.SendAsync(new SendNotificationRequest(
                RecipientUserId: user.Id,
                TemplateCode: "user.email_verification",
                Variables: new Dictionary<string, string> { ["token"] = rawToken }), ct);
        }

        return user.ToDto();
    }

    private static (string Raw, string Hash) GenerateToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var raw = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return (raw, hash);
    }
}

internal record TokenCacheData(Guid UserId, Guid CorporationId);
