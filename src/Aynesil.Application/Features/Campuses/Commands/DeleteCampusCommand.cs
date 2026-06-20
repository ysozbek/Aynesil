using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Campuses.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
/// <summary>Soft-deletes a campus (branch). All historical data referencing the campus is preserved.</summary>
public record DeleteCampusCommand(Guid Id) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────
public class DeleteCampusCommandValidator : AbstractValidator<DeleteCampusCommand>
{
    public DeleteCampusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class DeleteCampusCommandHandler : IRequestHandler<DeleteCampusCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCampusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCampusCommand req, CancellationToken ct)
    {
        var campus = await _db.Campuses
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Campus", req.Id);

        if (campus.IsDeleted)
            return; // idempotent

        campus.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
