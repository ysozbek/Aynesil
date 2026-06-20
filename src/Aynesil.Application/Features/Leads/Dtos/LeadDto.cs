namespace Aynesil.Application.Features.Leads.Dtos;

/// <summary>Full lead detail DTO. Returned by GetLead, Create, Update, and status-change operations.</summary>
public record LeadDto(
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
    DateOnly? ChildBirthDate,
    string ContactName,
    string? ContactPhone,
    string? ContactEmail,
    string? PresentingNeed,
    string? ReferralDetail,
    Guid? AssignedToId,
    string? AssignedToName,
    int? Score,
    Guid? ConvertedStudentId,
    DateTimeOffset? ConvertedAt,
    bool IsConverted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);
