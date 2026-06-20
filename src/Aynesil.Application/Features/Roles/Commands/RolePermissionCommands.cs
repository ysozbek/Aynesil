using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Permissions.Dtos;
using Aynesil.Domain.Modules.Iam.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Roles.Commands;

// ── Assign Permission ─────────────────────────────────────────────────────────

public record AssignRolePermissionCommand(Guid RoleId, Guid PermissionId) : IRequest<PermissionListItemDto>;

public class AssignRolePermissionCommandValidator : AbstractValidator<AssignRolePermissionCommand>
{
    public AssignRolePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermissionId).NotEmpty();
    }
}

public sealed class AssignRolePermissionCommandHandler : IRequestHandler<AssignRolePermissionCommand, PermissionListItemDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public AssignRolePermissionCommandHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PermissionListItemDto> Handle(AssignRolePermissionCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var role = await _db.Roles.FindAsync([req.RoleId], ct)
            ?? throw new NotFoundException("Role", req.RoleId);

        // Tenants may only modify their own roles, not system-level templates
        if (role.CorporationId.HasValue && role.CorporationId != corporationId)
            throw new UnauthorizedAccessException("You do not have access to this role.");

        var permission = await _db.Permissions.FindAsync([req.PermissionId], ct)
            ?? throw new NotFoundException("Permission", req.PermissionId);

        var alreadyAssigned = await _db.RolePermissions.AnyAsync(
            rp => rp.RoleId == req.RoleId && rp.PermissionId == req.PermissionId, ct);
        if (alreadyAssigned)
            return permission.ToListItemDto();

        _db.RolePermissions.Add(new RolePermission
        {
            RoleId = req.RoleId,
            PermissionId = req.PermissionId
        });

        await _db.SaveChangesAsync(ct);
        return permission.ToListItemDto();
    }
}

// ── Remove Permission ─────────────────────────────────────────────────────────

public record RemoveRolePermissionCommand(Guid RoleId, Guid PermissionId) : IRequest;

public class RemoveRolePermissionCommandValidator : AbstractValidator<RemoveRolePermissionCommand>
{
    public RemoveRolePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermissionId).NotEmpty();
    }
}

public sealed class RemoveRolePermissionCommandHandler : IRequestHandler<RemoveRolePermissionCommand>
{
    private readonly IAppDbContext _db;

    public RemoveRolePermissionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveRolePermissionCommand req, CancellationToken ct)
    {
        var grant = await _db.RolePermissions.FindAsync([req.RoleId, req.PermissionId], ct)
            ?? throw new NotFoundException("RolePermission", req.RoleId);

        _db.RolePermissions.Remove(grant);
        await _db.SaveChangesAsync(ct);
    }
}
