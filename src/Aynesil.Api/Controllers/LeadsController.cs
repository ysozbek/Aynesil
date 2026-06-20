using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Leads.Commands;
using Aynesil.Application.Features.Leads.Dtos;
using Aynesil.Application.Features.Leads.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// CRM / Lead management.
/// Leads are tenant-scoped (corporation_id required) and optionally branch-scoped (campus_id).
/// All reference-data lookups (source, status, pipeline stage) use configurable ref_value IDs.
/// Route: /api/leads
/// </summary>
[Route("api/leads")]
public sealed class LeadsController : BaseController
{
    // ── Lead Queries ──────────────────────────────────────────────────────────

    /// <summary>Returns a paginated, filterable list of leads.</summary>
    [HttpGet]
    [HasPermission(Permissions.Leads.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<LeadListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] Guid? statusId = null,
        [FromQuery] Guid? pipelineStageId = null,
        [FromQuery] Guid? sourceId = null,
        [FromQuery] Guid? assignedToId = null,
        [FromQuery] bool? isConverted = null,
        [FromQuery] bool hasPendingFollowUp = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "desc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetLeadsQuery
        {
            CorporationId = corporationId,
            CampusId = campusId,
            StatusId = statusId,
            PipelineStageId = pipelineStageId,
            SourceId = sourceId,
            AssignedToId = assignedToId,
            IsConverted = isConverted,
            HasPendingFollowUp = hasPendingFollowUp,
            Page = page,
            PageSize = pageSize,
            Search = search,
            SortBy = sortBy,
            SortDirection = sortDirection
        }, ct);

        return OkResult(result);
    }

    /// <summary>Returns the full details of a single lead.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Leads.Read)]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetLeadQuery(id), ct);
        return OkResult(result);
    }

    /// <summary>
    /// Returns the pipeline funnel summary for the CRM dashboard.
    /// Provides lead counts per pipeline stage plus converted/lost totals.
    /// </summary>
    [HttpGet("pipeline")]
    [HasPermission(Permissions.Leads.Read)]
    [ProducesResponseType(typeof(ApiResponse<PipelineSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPipelineSummary(
        [FromQuery] Guid corporationId,
        [FromQuery] Guid? campusId = null,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetPipelineSummaryQuery(corporationId, campusId), ct);
        return OkResult(result);
    }

    /// <summary>
    /// Returns activities with overdue or upcoming follow-up dates.
    /// Used by the "Today's Follow-ups" dashboard widget.
    /// </summary>
    [HttpGet("followups")]
    [HasPermission(Permissions.Leads.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<LeadActivityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowUpsDue(
        [FromQuery] Guid corporationId,
        [FromQuery] Guid? campusId = null,
        [FromQuery] DateTimeOffset? dueBy = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetFollowUpsDueQuery
        {
            CorporationId = corporationId,
            CampusId = campusId,
            DueBy = dueBy,
            Page = page,
            PageSize = pageSize
        }, ct);

        return OkResult(result);
    }

    /// <summary>
    /// Returns the lead-to-student conversion rate report for a date range,
    /// broken down by lead source.
    /// </summary>
    [HttpGet("reports/conversion")]
    [HasPermission(Permissions.Leads.Read)]
    [ProducesResponseType(typeof(ApiResponse<ConversionReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversionReport(
        [FromQuery] Guid corporationId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? campusId = null,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetConversionReportQuery(corporationId, from, to, campusId), ct);
        return OkResult(result);
    }

    // ── Lead Sub-resource Queries ─────────────────────────────────────────────

    /// <summary>Returns the status and pipeline-stage change history for a lead.</summary>
    [HttpGet("{id:guid}/history")]
    [HasPermission(Permissions.Leads.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeadStatusHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusHistory(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetLeadStatusHistoryQuery(id), ct);
        return OkResult(result);
    }

    /// <summary>Returns a paginated communication activity log for a lead.</summary>
    [HttpGet("{id:guid}/activities")]
    [HasPermission(Permissions.LeadActivities.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<LeadActivityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivities(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetLeadActivitiesQuery { LeadId = id, Page = page, PageSize = pageSize }, ct);
        return OkResult(result);
    }

    /// <summary>Returns all interviews scheduled for a lead.</summary>
    [HttpGet("{id:guid}/interviews")]
    [HasPermission(Permissions.Interviews.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InterviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInterviews(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetLeadInterviewsQuery(id), ct);
        return OkResult(result);
    }

    // ── Lead Commands ─────────────────────────────────────────────────────────

    /// <summary>Registers a new lead in the CRM pipeline.</summary>
    [HttpPost]
    [HasPermission(Permissions.Leads.Create)]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateLeadRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateLeadCommand(
            req.CorporationId, req.ContactName, req.CampusId,
            req.SourceId, req.StatusId, req.PipelineStageId,
            req.ChildName, req.ChildBirthDate,
            req.ContactPhone, req.ContactEmail,
            req.PresentingNeed, req.ReferralDetail,
            req.AssignedToId, req.Score), ct);

        return CreatedResult(result, $"/api/leads/{result.Id}");
    }

    /// <summary>Updates the lead's contact and classification details.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Leads.Update)]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateLeadCommand(
            id, req.ContactName, req.CampusId, req.SourceId,
            req.ChildName, req.ChildBirthDate,
            req.ContactPhone, req.ContactEmail,
            req.PresentingNeed, req.ReferralDetail,
            req.AssignedToId, req.Score, req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>Soft-deletes the lead. Converted leads cannot be deleted.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Leads.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteLeadCommand(id), ct);
        return NoContentResult("Lead deleted.");
    }

    /// <summary>
    /// Moves the lead to a new qualification status and optionally advances the pipeline stage.
    /// Appends a status history record.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [HasPermission(Permissions.Leads.Update)]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeLeadStatusRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new ChangeLeadStatusCommand(id, req.NewStatusId, req.NewPipelineStageId, req.RowVersion), ct);
        return OkResult(result);
    }

    /// <summary>Assigns the lead to a staff member.</summary>
    [HttpPost("{id:guid}/assign")]
    [HasPermission(Permissions.Leads.Assign)]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignLeadRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AssignLeadCommand(id, req.UserId, req.RowVersion), ct);
        return OkResult(result);
    }

    /// <summary>
    /// Links the lead to a student record, completing the conversion workflow.
    /// The student must be created by the Students module before calling this endpoint.
    /// </summary>
    [HttpPost("{id:guid}/convert")]
    [HasPermission(Permissions.Leads.Convert)]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertLeadRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new ConvertLeadToStudentCommand(id, req.StudentId, req.RowVersion), ct);
        return OkResult(result);
    }

    /// <summary>Logs a communication activity (call, email, SMS, note, visit) against the lead.</summary>
    [HttpPost("{id:guid}/activities")]
    [HasPermission(Permissions.LeadActivities.Create)]
    [ProducesResponseType(typeof(ApiResponse<LeadActivityDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LogActivity(Guid id, [FromBody] LogActivityRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new LogLeadActivityCommand(
            id, req.ActivityTypeId, req.Subject, req.Body,
            req.Direction, req.OccurredAt, req.FollowUpAt, req.PerformedBy), ct);

        return CreatedResult(result, $"/api/leads/{id}/activities");
    }

    /// <summary>Schedules a pre-enrollment interview for the lead.</summary>
    [HttpPost("{id:guid}/interviews")]
    [HasPermission(Permissions.Interviews.Create)]
    [ProducesResponseType(typeof(ApiResponse<InterviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ScheduleInterview(Guid id, [FromBody] ScheduleInterviewRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new ScheduleInterviewCommand(id, req.CampusId, req.ScheduledAt), ct);
        return CreatedResult(result, $"/api/interviews/{result.Id}");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateLeadRequest(
    Guid CorporationId,
    string ContactName,
    Guid? CampusId,
    Guid? SourceId,
    Guid? StatusId,
    Guid? PipelineStageId,
    string? ChildName,
    DateOnly? ChildBirthDate,
    string? ContactPhone,
    string? ContactEmail,
    string? PresentingNeed,
    string? ReferralDetail,
    Guid? AssignedToId,
    int? Score);

public record UpdateLeadRequest(
    string ContactName,
    Guid? CampusId,
    Guid? SourceId,
    string? ChildName,
    DateOnly? ChildBirthDate,
    string? ContactPhone,
    string? ContactEmail,
    string? PresentingNeed,
    string? ReferralDetail,
    Guid? AssignedToId,
    int? Score,
    int RowVersion);

public record ChangeLeadStatusRequest(
    Guid NewStatusId,
    Guid? NewPipelineStageId,
    int RowVersion);

public record AssignLeadRequest(Guid UserId, int RowVersion);

public record ConvertLeadRequest(Guid StudentId, int RowVersion);

public record LogActivityRequest(
    Guid? ActivityTypeId,
    string? Subject,
    string? Body,
    string? Direction,
    DateTimeOffset? OccurredAt,
    DateTimeOffset? FollowUpAt,
    Guid? PerformedBy);

public record ScheduleInterviewRequest(Guid? CampusId, DateTimeOffset? ScheduledAt);
