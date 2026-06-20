namespace Aynesil.Application.Features.Assessment.Dtos;

/// <summary>Compact template projection for paginated list views.</summary>
public record AssessmentTemplateListItemDto(
    Guid Id,
    Guid? CorporationId,
    string Code,
    string Name,
    Guid? TypeId,
    string? TypeCode,
    Guid? CategoryId,
    string? CategoryCode,
    string? ScoringModel,
    int Version,
    bool IsActive,
    int SectionCount,
    DateTimeOffset CreatedAt
);
