namespace Aynesil.Application.Features.Assessment.Dtos;

/// <summary>Program recommendation DTO — returned by recommendation queries and commands.</summary>
public record ProgramRecommendationDto(
    Guid Id,
    Guid CorporationId,
    Guid? AssessmentSessionId,
    Guid? LeadId,
    Guid? StudentId,
    Guid? RecommendedProgramId,
    string? RecommendedIntensity,
    string? Rationale,
    Guid? RecommendedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);
