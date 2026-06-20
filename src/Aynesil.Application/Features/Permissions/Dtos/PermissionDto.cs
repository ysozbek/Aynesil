namespace Aynesil.Application.Features.Permissions.Dtos;

/// <summary>Full permission detail. Permissions are platform-global and read-only for tenants.</summary>
public record PermissionDto(
    Guid Id,
    string Code,
    string Resource,
    string Action,
    string? Description
);
