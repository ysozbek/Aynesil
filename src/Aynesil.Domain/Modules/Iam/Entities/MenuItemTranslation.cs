namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.menu_item_translation. Composite PK (menu_item_id, locale).
/// Localized display label for a menu item.
/// The frontend loads translations via the /api/localization endpoint and
/// uses Vue I18n to render labels in the user's preferred locale.
/// </summary>
public class MenuItemTranslation
{
    public Guid MenuItemId { get; set; }

    /// <summary>BCP-47 locale code. FK to ref.locale.</summary>
    public string Locale { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public MenuItem? MenuItem { get; set; }
}
