namespace Aynesil.Application.Features.Permissions.Dtos;

/// <summary>Compact permission projection. Identical shape to PermissionDto — permissions have no heavyweight fields.</summary>
public record PermissionListItemDto(
    Guid Id,
    string Code,
    string Resource,
    string Action,
    string? Description
);
