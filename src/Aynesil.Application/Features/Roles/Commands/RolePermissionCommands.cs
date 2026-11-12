using Aynesil.Domain.Common;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Validation;
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
        var corporationId = _tenantContext.RequireCorporationId();

        var role = await _db.Roles.FindAsync([req.RoleId], ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Role"));

        // Tenants may only modify their own roles — not global (system) roles shared by every tenant.
        TenantOwnership.EnsureWritable(role.CorporationId, corporationId, "Role");

        var permission = await _db.Permissions.FindAsync([req.PermissionId], ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Permission"));

        // Platform operations (create / activate / suspend corporations) are not tenant permissions (Y1).
        if (Aynesil.Shared.Constants.Permissions.PlatformOnly.Contains(permission.Code))
            throw new ForbiddenAccessException(MsgKeys.Rule.PlatformPermissionNotAssignable, permission.Code);

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
    private readonly ITenantContext _tenantContext;

    public RemoveRolePermissionCommandHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task Handle(RemoveRolePermissionCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.RequireCorporationId();

        // iam.role_permission has no corporation_id: the role decides the tenant (Y2).
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == req.RoleId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Role"));
        TenantOwnership.EnsureWritable(role.CorporationId, corporationId, "Role");

        var grant = await _db.RolePermissions.FindAsync([req.RoleId, req.PermissionId], ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "RolePermission"));

        _db.RolePermissions.Remove(grant);
        await _db.SaveChangesAsync(ct);
    }
}
