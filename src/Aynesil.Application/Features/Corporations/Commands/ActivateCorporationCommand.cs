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

    public ActivateCorporationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateCorporationCommand req, CancellationToken ct)
    {
        var corp = await _db.Corporations
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Corporation", req.Id);

        if (corp.Status == "closed")
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.Id), "A closed corporation cannot be reactivated.")]);

        corp.Activate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
