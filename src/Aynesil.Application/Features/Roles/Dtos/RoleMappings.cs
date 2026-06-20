using Aynesil.Application.Features.Permissions.Dtos;
using Aynesil.Domain.Modules.Iam.Entities;

namespace Aynesil.Application.Features.Roles.Dtos;

/// <summary>Mapping extension methods for Role → DTOs.</summary>
public static class RoleMappings
{
    public static RoleListItemDto ToListItemDto(this Role r) =>
        new(r.Id, r.CorporationId, r.Code, r.Name, r.Description,
            r.IsSystem, r.RolePermissions.Count, r.CreatedAt);

    public static RoleDto ToDto(this Role r, IReadOnlyList<PermissionListItemDto> permissions) =>
        new(r.Id, r.CorporationId, r.Code, r.Name, r.Description,
            r.IsSystem, permissions, r.CreatedAt, r.UpdatedAt, r.RowVersion);
}
