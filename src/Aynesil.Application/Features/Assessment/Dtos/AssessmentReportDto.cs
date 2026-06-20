namespace Aynesil.Application.Features.Assessment.Dtos;

/// <summary>Assessment report DTO — returned by report queries and commands.</summary>
public record AssessmentReportDto(
    Guid Id,
    Guid CorporationId,
    Guid AssessmentSessionId,
    string? Summary,
    string? Findings,
    Guid? FileId,
    DateTimeOffset? FinalizedAt,
    Guid? FinalizedBy,
    bool IsFinalized,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);
