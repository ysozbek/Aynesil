namespace Aynesil.Application.Features.Assessment.Dtos;

/// <summary>Full session detail — includes responses, returned by GetSession and workflow commands.</summary>
public record AssessmentSessionDto(
    Guid Id,
    Guid CorporationId,
    Guid TemplateId,
    string? TemplateName,
    int TemplateVersion,
    Guid? LeadId,
    Guid? StudentId,
    Guid? CampusId,
    string? CampusName,
    Guid? AssessorId,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? PerformedAt,
    string Status,
    decimal? TotalScore,
    IReadOnlyList<AssessmentResponseDto> Responses,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);

/// <summary>Compact session projection for paginated list views.</summary>
public record AssessmentSessionListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid TemplateId,
    string? TemplateName,
    int TemplateVersion,
    Guid? LeadId,
    Guid? StudentId,
    Guid? CampusId,
    string? CampusName,
    Guid? AssessorId,
    DateTimeOffset? ScheduledAt,
    string Status,
    decimal? TotalScore,
    DateTimeOffset CreatedAt
);

/// <summary>Single evaluator response within a session.</summary>
public record AssessmentResponseDto(
    Guid Id,
    Guid AssessmentSessionId,
    Guid ItemId,
    string? ItemCode,
    decimal? NumericValue,
    string? TextValue,
    string? ChoiceValue,
    string? Note
);
