using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Campuses.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
/// <summary>Deactivates a campus (branch). Existing data is preserved; new scheduling/operations referencing it are blocked.</summary>
public record DeactivateCampusCommand(Guid Id) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────
public class DeactivateCampusCommandValidator : AbstractValidator<DeactivateCampusCommand>
{
    public DeactivateCampusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class DeactivateCampusCommandHandler : IRequestHandler<DeactivateCampusCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeactivateCampusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeactivateCampusCommand req, CancellationToken ct)
    {
        var campus = await _db.Campuses
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Campus", req.Id);

        if (!campus.IsActive)
            return; // idempotent

        campus.Deactivate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
