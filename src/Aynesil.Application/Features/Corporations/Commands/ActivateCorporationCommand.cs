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
/// <summary>Transitions a corporation's status to 'active'.</summary>
public record ActivateCorporationCommand(Guid Id) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────
public class ActivateCorporationCommandValidator : AbstractValidator<ActivateCorporationCommand>
{
    public ActivateCorporationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class ActivateCorporationCommandHandler : IRequestHandler<ActivateCorporationCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public ActivateCorporationCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task Handle(ActivateCorporationCommand req, CancellationToken ct)
    {
        // Only the caller's own corporation; any other id is "not found" (Y1). RLS enforces the same.
        var corporationId = _tenant.RequireCorporationId();

        var corp = await _db.Corporations
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.Id == corporationId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Corporation"));

        if (corp.Status == "closed")
            throw new ValidationException([ValidationFailures.For(nameof(req.Id), MsgKeys.Rule.ClosedCorporationNoReactivate)]);

        corp.Activate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
