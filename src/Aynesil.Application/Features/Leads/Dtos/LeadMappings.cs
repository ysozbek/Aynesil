using Aynesil.Domain.Modules.Crm.Entities;

namespace Aynesil.Application.Features.Leads.Dtos;

internal static class LeadMappings
{
    internal static LeadDto ToDto(
        this Lead lead,
        string? campusName,
        string? sourceCode,
        string? statusCode,
        string? pipelineStageCode,
        string? assignedToName)
        => new(
            lead.Id,
            lead.CorporationId,
            lead.CampusId,
            campusName,
            lead.SourceId,
            sourceCode,
            lead.StatusId,
            statusCode,
            lead.PipelineStageId,
            pipelineStageCode,
            lead.ChildName,
            lead.ChildBirthDate,
            lead.ContactName,
            lead.ContactPhone,
            lead.ContactEmail,
            lead.PresentingNeed,
            lead.ReferralDetail,
            lead.AssignedToId,
            assignedToName,
            lead.Score,
            lead.ConvertedStudentId,
            lead.ConvertedAt,
            lead.ConvertedStudentId.HasValue,
            lead.CreatedAt,
            lead.UpdatedAt,
            lead.RowVersion);

    internal static LeadActivityDto ToDto(
        this LeadActivity activity,
        string? activityTypeCode,
        string? performedByName)
        => new(
            activity.Id,
            activity.LeadId,
            activity.ActivityTypeId,
            activityTypeCode,
            activity.Subject,
            activity.Body,
            activity.Direction,
            activity.OccurredAt,
            activity.FollowUpAt,
            activity.PerformedBy,
            performedByName,
            activity.CreatedAt);

    internal static InterviewDto ToDto(
        this Interview interview,
        string? campusName,
        string? conductedByName)
        => new(
            interview.Id,
            interview.LeadId,
            interview.CampusId,
            campusName,
            interview.ScheduledAt,
            interview.ConductedAt,
            interview.ConductedBy,
            conductedByName,
            interview.Outcome,
            interview.Recommendation,
            interview.Status,
            interview.CreatedAt,
            interview.UpdatedAt,
            interview.RowVersion);

    internal static LeadStatusHistoryDto ToDto(
        this LeadStatusHistory h,
        string? statusCode,
        string? pipelineStageCode)
        => new(
            h.Id,
            h.StatusId,
            statusCode,
            h.PipelineStageId,
            pipelineStageCode,
            h.ChangedAt,
            h.ChangedBy);
}
