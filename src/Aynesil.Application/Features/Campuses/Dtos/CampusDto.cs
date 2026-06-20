namespace Aynesil.Application.Features.Campuses.Dtos;

/// <summary>Full campus (branch) detail DTO. Returned by GetCampus and Create/Update operations.</summary>
public record CampusDto(
    Guid Id,
    Guid CorporationId,
    string CorporationDisplayName,
    string Code,
    string Name,
    string? AddressLine,
    string? City,
    string? District,
    string? Phone,
    string? Email,
    string? Timezone,
    decimal? GeoLat,
    decimal? GeoLng,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);
