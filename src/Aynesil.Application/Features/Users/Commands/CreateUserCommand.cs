using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Users.Dtos;
using Aynesil.Domain.Modules.Iam.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Users.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

/// <summary>
/// Admin creates a new user account within their corporation.
/// Unlike RegisterCommand, this activates the account immediately by default
/// and does not trigger the email verification flow unless explicitly requested.
/// </summary>
public record CreateUserCommand(
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    string? Password,
    string? PreferredLocale = null,
    Guid? PrimaryCampusId = null,
    bool ActivateImmediately = true) : IRequest<UserDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
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

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;

    public CreateUserCommandHandler(
        IAppDbContext db,
        IPasswordService passwords,
        ICurrentUserService currentUser,
        ITenantContext tenantContext)
    {
        _db = db;
        _passwords = passwords;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public async Task<UserDto> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

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
            activateImmediately: req.ActivateImmediately);

        _db.UserAccounts.Add(user);
        await _db.SaveChangesAsync(ct);

        return user.ToDto();
    }
}
