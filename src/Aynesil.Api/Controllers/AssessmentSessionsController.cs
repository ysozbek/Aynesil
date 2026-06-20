using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Assessment.Commands;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Application.Features.Assessment.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Assessment session workflow — scheduling, evaluation, scoring, reporting, and recommendations.
/// Route: /api/assessment-sessions
///
/// Session lifecycle:
///   POST /                 → planned
///   POST /{id}/start       → in_progress
///   POST /{id}/responses   → upsert responses (repeatable)
///   POST /{id}/complete    → completed  (calculates score)
///   POST /{id}/cancel      → cancelled  (from planned or in_progress)
///
/// After completion:
///   POST /{id}/report                → create draft report
///   PUT  /{id}/report                → edit draft report
///   POST /{id}/report/finalize       → lock report (immutable)
///   POST /{id}/recommendations       → add program recommendation
///
/// History:
///   GET /history?leadId=&studentId=  → reassessment timeline for a lead or student
/// </summary>
[Route("api/assessment-sessions")]
public sealed class AssessmentSessionsController : BaseController
{
    // ── Session Queries ───────────────────────────────────────────────────────

    /// <summary>Returns a paginated, filterable list of assessment sessions.</summary>
    [HttpGet]
    [HasPermission(Permissions.AssessmentSessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AssessmentSessionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] Guid? templateId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? leadId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? assessorId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "desc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetAssessmentSessionsQuery
        {
            CorporationId = corporationId,
            CampusId      = campusId,
            TemplateId    = templateId,
            Status        = status,
            LeadId        = leadId,
            StudentId     = studentId,
            AssessorId    = assessorId,
            From          = from,
            To            = to,
            Page          = page,
            PageSize      = pageSize,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);

        return OkResult(result);
    }

    /// <summary>Returns full details of one session including all evaluator responses.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.AssessmentSessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetAssessmentSessionQuery(id), ct);
        return OkResult(result);
    }

    /// <summary>
    /// Returns all assessment sessions for a lead or student — the reassessment history.
    /// Exactly one of leadId or studentId must be provided.
    /// </summary>
    [HttpGet("history")]
    [HasPermission(Permissions.AssessmentSessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AssessmentSessionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid? leadId = null,
        [FromQuery] Guid? studentId = null,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetAssessmentHistoryQuery(leadId, studentId), ct);
        return OkResult(result);
    }

    // ── Session Commands ──────────────────────────────────────────────────────

    /// <summary>Schedules a new assessment session (status = planned).</summary>
    [HttpPost]
    [HasPermission(Permissions.AssessmentSessions.Create)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentSessionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssessmentSessionRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateAssessmentSessionCommand(
            req.CorporationId, req.TemplateId,
            req.LeadId, req.StudentId,
            req.CampusId, req.AssessorId, req.ScheduledAt), ct);

        return CreatedResult(result, $"/api/assessment-sessions/{result.Id}");
    }

    /// <summary>Updates the scheduled date, assessor, and campus for a planned or in-progress session.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.AssessmentSessions.Update)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateAssessmentSessionRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateAssessmentSessionCommand(
            id, req.ScheduledAt, req.AssessorId, req.CampusId, req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>Soft-deletes the session. Only sessions in 'planned' status can be deleted.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.AssessmentSessions.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteAssessmentSessionCommand(id), ct);
        return NoContentResult("Session deleted.");
    }

    // ── Workflow Transitions ──────────────────────────────────────────────────

    /// <summary>Transitions a planned session to in_progress, allowing response submission.</summary>
    [HttpPost("{id:guid}/start")]
    [HasPermission(Permissions.AssessmentSessions.Start)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Start(
        Guid id, [FromBody] WorkflowRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new StartAssessmentSessionCommand(id, req.RowVersion), ct);
        return OkResult(result);
    }

    /// <summary>
    /// Completes the session and calculates the total score.
    /// Scoring: sum | average of (numericValue × weight) per template scoring model.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [HasPermission(Permissions.AssessmentSessions.Complete)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Complete(
        Guid id, [FromBody] WorkflowRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CompleteAssessmentSessionCommand(id, req.RowVersion), ct);
        return OkResult(result);
    }

    /// <summary>Cancels a planned or in-progress session.</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.AssessmentSessions.Cancel)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] WorkflowRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CancelAssessmentSessionCommand(id, req.RowVersion), ct);
        return OkResult(result);
    }

    // ── Responses ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Upserts evaluator responses for an in-progress session.
    /// Can be called multiple times — existing responses are updated, new ones are inserted.
    /// </summary>
    [HttpPost("{id:guid}/responses")]
    [HasPermission(Permissions.AssessmentSessions.SubmitResponses)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AssessmentResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitResponses(
        Guid id, [FromBody] SubmitResponsesRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new SubmitResponsesCommand(
            id,
            req.Responses.Select(r =>
                new ResponseInput(r.ItemId, r.NumericValue, r.TextValue, r.ChoiceValue, r.Note))
                .ToList().AsReadOnly()), ct);

        return OkResult(result);
    }

    // ── Report ────────────────────────────────────────────────────────────────

    /// <summary>Returns the clinical report for this session, or null if none exists yet.</summary>
    [HttpGet("{id:guid}/report")]
    [HasPermission(Permissions.AssessmentReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetAssessmentReportQuery(id), ct);
        return OkResult(result);
    }

    /// <summary>Creates a draft report for a completed session.</summary>
    [HttpPost("{id:guid}/report")]
    [HasPermission(Permissions.AssessmentReports.Create)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateReport(
        Guid id, [FromBody] CreateReportRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateAssessmentReportCommand(
            req.CorporationId, id,
            req.Summary, req.Findings, req.FileId), ct);

        return CreatedResult(result, $"/api/assessment-sessions/{id}/report");
    }

    /// <summary>Updates the draft report's summary, findings, and attached file.</summary>
    [HttpPut("{id:guid}/report")]
    [HasPermission(Permissions.AssessmentReports.Update)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateReport(
        Guid id, [FromBody] UpdateReportRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateAssessmentReportCommand(
            req.ReportId, req.Summary, req.Findings, req.FileId, req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>
    /// Finalizes the report, locking it as immutable.
    /// Raises AssessmentReportFinalizedEvent for downstream consumers.
    /// </summary>
    [HttpPost("{id:guid}/report/finalize")]
    [HasPermission(Permissions.AssessmentReports.Finalize)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> FinalizeReport(
        Guid id, [FromBody] FinalizeReportRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new FinalizeAssessmentReportCommand(req.ReportId, req.RowVersion), ct);

        return OkResult(result);
    }

    // ── Program Recommendations ───────────────────────────────────────────────

    /// <summary>Returns all program recommendations for this session.</summary>
    [HttpGet("{id:guid}/recommendations")]
    [HasPermission(Permissions.ProgramRecommendations.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProgramRecommendationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendations(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(
            new GetProgramRecommendationsQuery(id, null, null), ct);

        return OkResult(result);
    }

    /// <summary>
    /// Creates a program recommendation from this session.
    /// Raises ProgramRecommendationCreatedEvent — consumed by the enrollment module
    /// to pre-fill the enrollment form with the recommended program.
    /// </summary>
    [HttpPost("{id:guid}/recommendations")]
    [HasPermission(Permissions.ProgramRecommendations.Create)]
    [ProducesResponseType(typeof(ApiResponse<ProgramRecommendationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateRecommendation(
        Guid id, [FromBody] CreateRecommendationRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateProgramRecommendationCommand(
            req.CorporationId, id,
            req.LeadId, req.StudentId,
            req.RecommendedProgramId,
            req.RecommendedIntensity,
            req.Rationale,
            req.RecommendedBy), ct);

        return CreatedResult(result, $"/api/assessment-sessions/{id}/recommendations");
    }

    /// <summary>Updates an existing recommendation's program, intensity, and rationale.</summary>
    [HttpPut("{id:guid}/recommendations/{recommendationId:guid}")]
    [HasPermission(Permissions.ProgramRecommendations.Update)]
    [ProducesResponseType(typeof(ApiResponse<ProgramRecommendationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRecommendation(
        Guid id, Guid recommendationId,
        [FromBody] UpdateRecommendationRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateProgramRecommendationCommand(
            recommendationId,
            req.RecommendedProgramId,
            req.RecommendedIntensity,
            req.Rationale,
            req.RecommendedBy,
            req.RowVersion), ct);

        return OkResult(result);
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateAssessmentSessionRequest(
    Guid CorporationId,
    Guid TemplateId,
    Guid? LeadId,
    Guid? StudentId,
    Guid? CampusId,
    Guid? AssessorId,
    DateTimeOffset? ScheduledAt);

public record UpdateAssessmentSessionRequest(
    DateTimeOffset? ScheduledAt,
    Guid? AssessorId,
    Guid? CampusId,
    int RowVersion);

public record WorkflowRequest(int RowVersion);

public record SubmitResponsesRequest(IReadOnlyList<ResponseItemRequest> Responses);

public record ResponseItemRequest(
    Guid ItemId,
    decimal? NumericValue,
    string? TextValue,
    string? ChoiceValue,
    string? Note);

public record CreateReportRequest(
    Guid CorporationId,
    string? Summary,
    string? Findings,
    Guid? FileId);

public record UpdateReportRequest(
    Guid ReportId,
    string? Summary,
    string? Findings,
    Guid? FileId,
    int RowVersion);

public record FinalizeReportRequest(Guid ReportId, int RowVersion);

public record CreateRecommendationRequest(
    Guid CorporationId,
    Guid? LeadId,
    Guid? StudentId,
    Guid? RecommendedProgramId,
    string? RecommendedIntensity,
    string? Rationale,
    Guid? RecommendedBy);

public record UpdateRecommendationRequest(
    Guid? RecommendedProgramId,
    string? RecommendedIntensity,
    string? Rationale,
    Guid? RecommendedBy,
    int RowVersion);
