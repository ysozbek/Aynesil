using Aynesil.Domain.Modules.Iam.Entities;

namespace Aynesil.Application.Features.Permissions.Dtos;

/// <summary>Mapping extension methods for Permission → DTOs.</summary>
public static class PermissionMappings
{
    public static PermissionDto ToDto(this Permission p) =>
        new(p.Id, p.Code, p.Resource, p.Action, p.Description);

    public static PermissionListItemDto ToListItemDto(this Permission p) =>
        new(p.Id, p.Code, p.Resource, p.Action, p.Description);
}
