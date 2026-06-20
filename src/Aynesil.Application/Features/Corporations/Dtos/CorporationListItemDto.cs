namespace Aynesil.Application.Features.Corporations.Dtos;

/// <summary>Lightweight projection used in the paginated corporations list.</summary>
public record CorporationListItemDto(
    Guid Id,
    string Code,
    string LegalName,
    string DisplayName,
    string DefaultLocale,
    string DefaultCurrency,
    string Timezone,
    string Status,
    int CampusCount,
    DateTimeOffset CreatedAt
);
