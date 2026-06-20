using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Roles.Dtos;
using FluentValidation;
using MediatR;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Roles.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

public record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string? Description,
    int RowVersion) : IRequest<RoleListItemDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleListItemDto>
{
    private readonly IAppDbContext _db;

    public UpdateRoleCommandHandler(IAppDbContext db) => _db = db;

    public async Task<RoleListItemDto> Handle(UpdateRoleCommand req, CancellationToken ct)
    {
        var role = await _db.Roles.FindAsync([req.RoleId], ct)
            ?? throw new NotFoundException("Role", req.RoleId);

        if (req.RowVersion != role.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The record was modified by another user. Please reload and try again.")]);

        role.Update(req.Name, req.Description);
        await _db.SaveChangesAsync(ct);

        return role.ToListItemDto();
    }
}
