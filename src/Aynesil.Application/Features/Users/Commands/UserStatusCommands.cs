using Aynesil.Application.Common.Validation;
using Aynesil.Domain.Common;
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
    private readonly ISessionRevocation _sessions;

    public ActivateUserCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ISessionRevocation sessions)
    {
        _sessions = sessions;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateUserCommand req, CancellationToken ct)
    {
        var user = await _db.UserAccounts.FindAsync([req.UserId], ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "User"));

        user.Activate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        _sessions.Forget(req.UserId);   // önbellekteki "askıda" durumu yeni girişi 1 dk engellemesin
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
    private readonly ISessionRevocation _sessions;

    public SuspendUserCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITokenService tokens, ISessionRevocation sessions)
    {
        _sessions = sessions;
        _db = db;
        _currentUser = currentUser;
        _tokens = tokens;
    }

    public async Task Handle(SuspendUserCommand req, CancellationToken ct)
    {
        var user = await _db.UserAccounts.FindAsync([req.UserId], ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "User"));

        if (_currentUser.UserId == req.UserId)
            throw new ValidationException([ValidationFailures.For(nameof(req.UserId), MsgKeys.Rule.CannotSuspendSelf)]);

        user.Suspend(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        // Yenileme jetonları iptal edilir; erişim jetonu domain'de (Suspend → sessions_revoked_at, V63) geçersiz
        // sayılır ve önbellek temizlenince bir sonraki istekte 401 alır (önceden süresi dolana kadar geçerliydi).
        await _tokens.RevokeAllAsync(req.UserId, ct);
        _sessions.Forget(req.UserId);
    }
}
