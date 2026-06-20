namespace Aynesil.Application.Features.Leads.Dtos;

/// <summary>Communication / activity log entry for a lead.</summary>
public record LeadActivityDto(
    Guid Id,
    Guid LeadId,
    Guid? ActivityTypeId,
    string? ActivityTypeCode,
    string? Subject,
    string? Body,
    string? Direction,
    DateTimeOffset OccurredAt,
    DateTimeOffset? FollowUpAt,
    Guid? PerformedBy,
    string? PerformedByName,
    DateTimeOffset CreatedAt
);
