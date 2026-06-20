namespace Aynesil.Application.Features.Leads.Dtos;

/// <summary>Pre-enrollment interview DTO.</summary>
public record InterviewDto(
    Guid Id,
    Guid LeadId,
    Guid? CampusId,
    string? CampusName,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? ConductedAt,
    Guid? ConductedBy,
    string? ConductedByName,
    string? Outcome,
    string? Recommendation,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);
