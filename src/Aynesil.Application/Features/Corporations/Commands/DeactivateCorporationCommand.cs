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

    public DeactivateCorporationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeactivateCorporationCommand req, CancellationToken ct)
    {
        var corp = await _db.Corporations
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Corporation", req.Id);

        if (corp.Status == "closed")
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.Id), "A closed corporation cannot be suspended.")]);

        corp.Suspend(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
