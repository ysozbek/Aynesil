namespace Aynesil.Application.Features.Roles.Dtos;

/// <summary>Compact role projection for list screens.</summary>
public record RoleListItemDto(
    Guid Id,
    Guid? CorporationId,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    int PermissionCount,
    DateTimeOffset CreatedAt
);
