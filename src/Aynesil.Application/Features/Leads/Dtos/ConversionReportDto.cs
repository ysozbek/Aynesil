namespace Aynesil.Application.Features.Leads.Dtos;

/// <summary>
/// Lead-to-student conversion rate report for a date range.
/// Breaks down total vs converted counts globally and per lead source.
/// </summary>
public record ConversionReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalLeads,
    int TotalConverted,
    decimal ConversionRate,
    IReadOnlyList<ConversionBySourceDto> BySource
);

/// <summary>Conversion statistics for a single lead source.</summary>
public record ConversionBySourceDto(
    Guid? SourceId,
    string? SourceCode,
    int TotalLeads,
    int Converted,
    decimal ConversionRate
);
