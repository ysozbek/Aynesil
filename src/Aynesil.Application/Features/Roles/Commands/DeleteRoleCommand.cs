using Aynesil.Application.Common.Validation;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Roles.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

public record DeleteRoleCommand(Guid RoleId) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator() => RuleFor(x => x.RoleId).NotEmpty();
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
{
    private readonly IAppDbContext _db;

    public DeleteRoleCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteRoleCommand req, CancellationToken ct)
    {
        var role = await _db.Roles.FindAsync([req.RoleId], ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Role"));

        // Block deletion if any users are still assigned this role
        var hasUsers = await _db.UserRoles.AnyAsync(ur => ur.RoleId == req.RoleId, ct);
        if (hasUsers)
            throw new ValidationException([ValidationFailures.For(nameof(req.RoleId), MsgKeys.Rule.RoleStillAssigned)]);

        role.SoftDelete();
        await _db.SaveChangesAsync(ct);
    }
}
