namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Resolves localized strings from ref.i18n_message (static UI text) and
/// ref.ref_value_translation (reference data labels) with fallback chain:
///   requested locale → corporation default → 'tr' → code.
/// Results are cached per corporation per locale.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Resolves a static UI message by namespace + key.
    /// Falls back through the locale chain if no translation exists.
    /// </summary>
    Task<string> GetMessageAsync(
        string @namespace,
        string key,
        string? locale = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all messages for a locale as a flat dictionary (for frontend i18n loading).</summary>
    Task<IReadOnlyDictionary<string, string>> GetMessagesAsync(
        string @namespace,
        string locale,
        Guid? corporationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves the localized label for a reference value.</summary>
    Task<string> GetRefValueLabelAsync(
        Guid refValueId,
        string? locale = null,
        CancellationToken cancellationToken = default);
}
