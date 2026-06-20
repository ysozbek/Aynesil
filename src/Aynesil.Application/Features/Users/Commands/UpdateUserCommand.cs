using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Users.Dtos;
using FluentValidation;
using MediatR;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Users.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

public record UpdateUserCommand(
    Guid UserId,
    string FullName,
    string? Phone,
    string? Email,
    string? PreferredLocale,
    Guid? PrimaryCampusId,
    int RowVersion) : IRequest<UserDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => x.Email is not null);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.PreferredLocale).MaximumLength(20).When(x => x.PreferredLocale is not null);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;

    public UpdateUserCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _db = db;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public async Task<UserDto> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var user = await _db.UserAccounts.FindAsync([req.UserId], ct)
            ?? throw new NotFoundException("User", req.UserId);

        if (req.RowVersion != user.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The record was modified by another user. Please reload and try again.")]);

        if (req.Email is not null && req.Email.ToLowerInvariant() != user.Email)
        {
            var corporationId = _tenantContext.CorporationId!.Value;
            var emailLower = req.Email.ToLowerInvariant();
            var taken = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                _db.UserAccounts, u => u.CorporationId == corporationId && u.Email == emailLower && u.Id != req.UserId, ct);
            if (taken)
                throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                    nameof(req.Email), $"Email '{emailLower}' is already registered.")]);
        }

        user.UpdateProfile(req.FullName, req.Phone, req.Email, req.PreferredLocale, req.PrimaryCampusId, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return user.ToDto();
    }
}
