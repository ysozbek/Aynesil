namespace Aynesil.Application.Features.Corporations.Dtos;

/// <summary>
/// Returns all configurable fields that govern how a corporation and its branches operate.
/// The Settings property is a raw JSON object — the frontend deserialises it as needed.
/// Locale, currency, and timezone are promoted as first-class fields for clarity.
/// </summary>
public record CorporationSettingsDto(
    Guid CorporationId,
    string Code,
    string DisplayName,
    string DefaultLocale,
    string DefaultCurrency,
    string Timezone,
    string? TaxOffice,
    string? TaxNumber,
    string Settings
);
