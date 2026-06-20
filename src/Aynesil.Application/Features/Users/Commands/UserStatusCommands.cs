using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Users.Commands;

// ── Activate ─────────────────────────────────────────────────────────────────

public record ActivateUserCommand(Guid UserId) : IRequest;

public class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}

public sealed class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ActivateUserCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateUserCommand req, CancellationToken ct)
    {
        var user = await _db.UserAccounts.FindAsync([req.UserId], ct)
            ?? throw new NotFoundException("User", req.UserId);

        user.Activate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Suspend ───────────────────────────────────────────────────────────────────

public record SuspendUserCommand(Guid UserId) : IRequest;

public class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
{
    public SuspendUserCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}

public sealed class SuspendUserCommandHandler : IRequestHandler<SuspendUserCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenService _tokens;

    public SuspendUserCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITokenService tokens)
    {
        _db = db;
        _currentUser = currentUser;
        _tokens = tokens;
    }

    public async Task Handle(SuspendUserCommand req, CancellationToken ct)
    {
        var user = await _db.UserAccounts.FindAsync([req.UserId], ct)
            ?? throw new NotFoundException("User", req.UserId);

        if (_currentUser.UserId == req.UserId)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.UserId), "You cannot suspend your own account.")]);

        user.Suspend(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        // Revoke active sessions so the user is kicked out immediately
        await _tokens.RevokeAllAsync(req.UserId, ct);
    }
}
