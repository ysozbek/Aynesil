using Aynesil.Application.Common.Validation;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Corporations.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
/// <summary>Transitions a corporation's status to 'suspended', preventing active use.</summary>
public record DeactivateCorporationCommand(Guid Id) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────
public class DeactivateCorporationCommandValidator : AbstractValidator<DeactivateCorporationCommand>
{
    public DeactivateCorporationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class DeactivateCorporationCommandHandler : IRequestHandler<DeactivateCorporationCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public DeactivateCorporationCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task Handle(DeactivateCorporationCommand req, CancellationToken ct)
    {
        // Only the caller's own corporation; any other id is "not found" (Y1). RLS enforces the same.
        var corporationId = _tenant.RequireCorporationId();

        var corp = await _db.Corporations
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.Id == corporationId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Corporation"));

        if (corp.Status == "closed")
            throw new ValidationException([ValidationFailures.For(nameof(req.Id), MsgKeys.Rule.ClosedCorporationNoSuspend)]);

        corp.Suspend(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
