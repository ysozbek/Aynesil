namespace Aynesil.Application.Features.Leads.Dtos;

/// <summary>Single entry in the lead's status/pipeline-stage change audit trail.</summary>
public record LeadStatusHistoryDto(
    Guid Id,
    Guid? StatusId,
    string? StatusCode,
    string? StatusName,
    Guid? PipelineStageId,
    string? PipelineStageCode,
    string? PipelineStageName,
    DateTimeOffset ChangedAt,
    Guid? ChangedBy,
    string? ChangedByName
);
