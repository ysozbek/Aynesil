using Aynesil.Application.Features.Permissions.Dtos;

namespace Aynesil.Application.Features.Roles.Dtos;

/// <summary>Full role detail DTO including assigned permissions. Returned by GetRole.</summary>
public record RoleDto(
    Guid Id,
    Guid? CorporationId,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<PermissionListItemDto> Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);
