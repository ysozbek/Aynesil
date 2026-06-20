using Aynesil.Domain.Modules.Core.Entities;

namespace Aynesil.Application.Features.Campuses.Dtos;

/// <summary>
/// Extension methods that project Campus domain entities to DTOs.
/// </summary>
internal static class CampusMappings
{
    internal static CampusDto ToDto(this Campus campus, string corporationDisplayName) =>
        new(
            campus.Id,
            campus.CorporationId,
            corporationDisplayName,
            campus.Code,
            campus.Name,
            campus.AddressLine,
            campus.City,
            campus.District,
            campus.Phone,
            campus.Email,
            campus.Timezone,
            campus.GeoLat,
            campus.GeoLng,
            campus.IsActive,
            campus.CreatedAt,
            campus.UpdatedAt,
            campus.RowVersion);

    internal static CampusListItemDto ToListItemDto(this Campus campus, string corporationDisplayName) =>
        new(
            campus.Id,
            campus.CorporationId,
            corporationDisplayName,
            campus.Code,
            campus.Name,
            campus.City,
            campus.District,
            campus.Phone,
            campus.IsActive,
            campus.CreatedAt);
}
