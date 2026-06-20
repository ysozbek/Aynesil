using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Campuses.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
/// <summary>Sets a campus (branch) to active, allowing operations to be performed under it.</summary>
public record ActivateCampusCommand(Guid Id) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────
public class ActivateCampusCommandValidator : AbstractValidator<ActivateCampusCommand>
{
    public ActivateCampusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class ActivateCampusCommandHandler : IRequestHandler<ActivateCampusCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ActivateCampusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateCampusCommand req, CancellationToken ct)
    {
        var campus = await _db.Campuses
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Campus", req.Id);

        if (campus.IsActive)
            return; // idempotent

        campus.Activate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
