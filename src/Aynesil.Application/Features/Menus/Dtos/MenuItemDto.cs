namespace Aynesil.Application.Features.Menus.Dtos;

/// <summary>Full detail DTO for a single menu item. Includes RowVersion for optimistic concurrency.</summary>
public record MenuItemDto(
    Guid Id,
    Guid? CorporationId,
    Guid? ParentId,
    string Code,
    string? Route,
    string? Icon,
    int SortOrder,
    Guid? RequiredPermissionId,
    string? RequiredPermissionCode,
    string? FeatureFlag,
    bool IsActive,
    IReadOnlyList<MenuItemTranslationDto> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);
