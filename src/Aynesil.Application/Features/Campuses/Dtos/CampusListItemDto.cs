namespace Aynesil.Application.Features.Campuses.Dtos;

/// <summary>Lightweight projection used in the paginated campus (branch) list.</summary>
public record CampusListItemDto(
    Guid Id,
    Guid CorporationId,
    string CorporationDisplayName,
    string Code,
    string Name,
    string? City,
    string? District,
    string? Phone,
    bool IsActive,
    DateTimeOffset CreatedAt
);
