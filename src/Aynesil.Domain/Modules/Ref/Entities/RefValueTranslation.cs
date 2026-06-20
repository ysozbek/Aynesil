namespace Aynesil.Domain.Modules.Ref.Entities;

/// <summary>
/// Maps to ref.ref_value_translation. Composite PK (ref_value_id, locale).
/// Localized labels for reference values.
/// Fallback chain is implemented by the DB function ref.value_label() and
/// mirrored in the application's ILocalizationService:
///   requested → corporation default → 'en' → value code.
/// </summary>
public class RefValueTranslation
{
    public Guid RefValueId { get; set; }

    /// <summary>BCP-47 locale code. FK to ref.locale.</summary>
    public string Locale { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>Abbreviated label for compact UI elements (e.g. table columns).</summary>
    public string? ShortLabel { get; set; }

    public string? Description { get; set; }

    public RefValue? RefValue { get; set; }
    public Locale? LocaleNavigation { get; set; }
}
