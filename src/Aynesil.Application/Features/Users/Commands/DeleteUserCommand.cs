using Aynesil.Application.Common.Validation;
using Aynesil.Domain.Common;
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
    private readonly ISessionRevocation _sessions;

    public DeleteUserCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITokenService tokens, ISessionRevocation sessions)
    {
        _sessions = sessions;
        _db = db;
        _currentUser = currentUser;
        _tokens = tokens;
    }

    public async Task Handle(DeleteUserCommand req, CancellationToken ct)
    {
        var user = await _db.UserAccounts.FindAsync([req.UserId], ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "User"));

        if (req.RowVersion != user.RowVersion)
            throw new ValidationException([ValidationFailures.For(nameof(req.RowVersion), MsgKeys.Error.Concurrency)]);

        if (_currentUser.UserId == req.UserId)
            throw new ValidationException([ValidationFailures.For(nameof(req.UserId), MsgKeys.Rule.CannotDeleteSelf)]);

        user.RevokeSessions();   // V63: elde kalan erişim jetonu da geçersiz
        user.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        // Revoke all active sessions for the deleted user
        await _tokens.RevokeAllAsync(req.UserId, ct);
        _sessions.Forget(req.UserId);
    }
}
