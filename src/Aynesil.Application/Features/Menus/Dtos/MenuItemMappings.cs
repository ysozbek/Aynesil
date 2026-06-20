using Aynesil.Domain.Modules.Iam.Entities;

namespace Aynesil.Application.Features.Menus.Dtos;

/// <summary>Mapping extension methods for MenuItem → DTOs.</summary>
public static class MenuItemMappings
{
    public static MenuItemListItemDto ToListItemDto(this MenuItem m) =>
        new(m.Id,
            m.CorporationId,
            m.ParentId,
            m.Code,
            m.Route,
            m.Icon,
            m.SortOrder,
            m.RequiredPermissionId,
            m.RequiredPermission?.Code,
            m.FeatureFlag,
            m.IsActive,
            m.Translations
                .OrderBy(t => t.Locale)
                .Select(t => new MenuItemTranslationDto(t.Locale, t.Label))
                .ToList(),
            m.CreatedAt,
            m.UpdatedAt);

    public static MenuItemDto ToDto(this MenuItem m) =>
        new(m.Id,
            m.CorporationId,
            m.ParentId,
            m.Code,
            m.Route,
            m.Icon,
            m.SortOrder,
            m.RequiredPermissionId,
            m.RequiredPermission?.Code,
            m.FeatureFlag,
            m.IsActive,
            m.Translations
                .OrderBy(t => t.Locale)
                .Select(t => new MenuItemTranslationDto(t.Locale, t.Label))
                .ToList(),
            m.CreatedAt,
            m.UpdatedAt,
            m.RowVersion);
}
