namespace Aynesil.Application.Features.Corporations.Dtos;

/// <summary>Full corporation detail DTO. Returned by GetCorporation and Create/Update operations.</summary>
public record CorporationDto(
    Guid Id,
    string Code,
    string LegalName,
    string DisplayName,
    string DefaultLocale,
    string DefaultCurrency,
    string Timezone,
    string? TaxOffice,
    string? TaxNumber,
    string Status,
    string Settings,
    int CampusCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);
