namespace Aynesil.Application.Features.Assessment.Dtos;

/// <summary>Full template detail — returned by GetTemplate, Create, Update operations.</summary>
public record AssessmentTemplateDto(
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
    IReadOnlyList<AssessmentTranslationDto> Translations,
    IReadOnlyList<AssessmentSectionDto> Sections,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);

/// <summary>Per-locale display text for a template.</summary>
public record AssessmentTranslationDto(
    string Locale,
    string Name,
    string? Description
);

/// <summary>Section within a template, with ordered items.</summary>
public record AssessmentSectionDto(
    Guid Id,
    Guid TemplateId,
    string Code,
    int SortOrder,
    Guid? DevelopmentAreaId,
    string? DevelopmentAreaCode,
    IReadOnlyList<AssessmentItemDto> Items
);

/// <summary>Individual assessment item within a section.</summary>
public record AssessmentItemDto(
    Guid Id,
    Guid SectionId,
    string Code,
    string Prompt,
    string ResponseType,
    string? Choices,
    decimal Weight,
    int SortOrder
);
