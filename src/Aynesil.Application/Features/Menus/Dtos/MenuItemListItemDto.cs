namespace Aynesil.Application.Features.Menus.Dtos;

/// <summary>Flat projection used in the admin management list and tree responses.</summary>
public record MenuItemListItemDto(
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
    DateTimeOffset UpdatedAt);
