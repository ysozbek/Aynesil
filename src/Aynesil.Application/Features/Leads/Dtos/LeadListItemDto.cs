namespace Aynesil.Application.Features.Leads.Dtos;

/// <summary>Compact lead projection for paginated list views and pipeline kanban columns.</summary>
public record LeadListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    string? CampusName,
    Guid? SourceId,
    string? SourceCode,
    Guid? StatusId,
    string? StatusCode,
    Guid? PipelineStageId,
    string? PipelineStageCode,
    string? ChildName,
    string ContactName,
    string? ContactPhone,
    string? ContactEmail,
    Guid? AssignedToId,
    string? AssignedToName,
    int? Score,
    bool IsConverted,
    DateTimeOffset CreatedAt
);
