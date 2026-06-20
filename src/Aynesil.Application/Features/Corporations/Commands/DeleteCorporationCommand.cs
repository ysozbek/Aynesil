using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Corporations.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
/// <summary>Soft-deletes a corporation. All data remains in the DB; RLS will hide it from the app.</summary>
public record DeleteCorporationCommand(Guid Id) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────
public class DeleteCorporationCommandValidator : AbstractValidator<DeleteCorporationCommand>
{
    public DeleteCorporationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class DeleteCorporationCommandHandler : IRequestHandler<DeleteCorporationCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCorporationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCorporationCommand req, CancellationToken ct)
    {
        var corp = await _db.Corporations
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Corporation", req.Id);

        if (corp.IsDeleted)
            return; // idempotent

        corp.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
