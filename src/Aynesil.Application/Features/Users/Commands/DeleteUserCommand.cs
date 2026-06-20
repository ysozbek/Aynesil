using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Iam.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Users.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

public record DeleteUserCommand(Guid UserId, int RowVersion) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenService _tokens;

    public DeleteUserCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITokenService tokens)
    {
        _db = db;
        _currentUser = currentUser;
        _tokens = tokens;
    }

    public async Task Handle(DeleteUserCommand req, CancellationToken ct)
    {
        var user = await _db.UserAccounts.FindAsync([req.UserId], ct)
            ?? throw new NotFoundException("User", req.UserId);

        if (req.RowVersion != user.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The record was modified by another user. Please reload and try again.")]);

        if (_currentUser.UserId == req.UserId)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.UserId), "You cannot delete your own account.")]);

        user.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        // Revoke all active sessions for the deleted user
        await _tokens.RevokeAllAsync(req.UserId, ct);
    }
}
