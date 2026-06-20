using Aynesil.Domain.Modules.Core.Entities;

namespace Aynesil.Application.Features.Corporations.Dtos;

/// <summary>
/// Extension methods that project Corporation domain entities to DTOs.
/// Kept close to the DTOs to avoid a separate AutoMapper profile for this straightforward mapping.
/// </summary>
internal static class CorporationMappings
{
    internal static CorporationDto ToDto(this Corporation corp, int campusCount) =>
        new(
            corp.Id,
            corp.Code,
            corp.LegalName,
            corp.DisplayName,
            corp.DefaultLocale,
            corp.DefaultCurrency,
            corp.Timezone,
            corp.TaxOffice,
            corp.TaxNumber,
            corp.Status,
            corp.Settings,
            campusCount,
            corp.CreatedAt,
            corp.UpdatedAt,
            corp.RowVersion);

    internal static CorporationListItemDto ToListItemDto(this Corporation corp, int campusCount) =>
        new(
            corp.Id,
            corp.Code,
            corp.LegalName,
            corp.DisplayName,
            corp.DefaultLocale,
            corp.DefaultCurrency,
            corp.Timezone,
            corp.Status,
            campusCount,
            corp.CreatedAt);

    internal static CorporationSettingsDto ToSettingsDto(this Corporation corp) =>
        new(
            corp.Id,
            corp.Code,
            corp.DisplayName,
            corp.DefaultLocale,
            corp.DefaultCurrency,
            corp.Timezone,
            corp.TaxOffice,
            corp.TaxNumber,
            corp.Settings);
}
