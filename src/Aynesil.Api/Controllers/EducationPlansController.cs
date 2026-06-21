using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Plans.Commands;
using Aynesil.Application.Features.Plans.Dtos;
using Aynesil.Application.Features.Plans.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Academic Periods, Education Plans (BEP/IEP) with full lifecycle,
/// Plan Goals management, Reviews, Approvals, Revisions, and Goal Reports.
/// Route: /api/education-plans
/// </summary>
[Route("api/education-plans")]
public sealed class EducationPlansController : BaseController
{
    // ── Academic Periods ──────────────────────────────────────────────────────

    [HttpGet("academic-periods")]
    [HasPermission(Permissions.AcademicPeriods.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AcademicPeriodListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAcademicPeriods(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] bool? isCurrent = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetAcademicPeriodsQuery
        {
            CorporationId = corporationId,
            IsCurrent     = isCurrent,
            Page          = page,
            PageSize      = pageSize,
            Search        = search,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("academic-periods/{id:guid}")]
    [HasPermission(Permissions.AcademicPeriods.Read)]
    [ProducesResponseType(typeof(ApiResponse<AcademicPeriodDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAcademicPeriod(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetAcademicPeriodQuery(id), ct));

    [HttpPost("academic-periods")]
    [HasPermission(Permissions.AcademicPeriods.Manage)]
    [ProducesResponseType(typeof(ApiResponse<AcademicPeriodDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAcademicPeriod(
        [FromBody] CreateAcademicPeriodRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateAcademicPeriodCommand(
            req.CorporationId, req.Name, req.StartDate, req.EndDate, req.TermId, req.IsCurrent), ct);
        return CreatedResult(result, $"/api/education-plans/academic-periods/{result.Id}");
    }

    [HttpPut("academic-periods/{id:guid}")]
    [HasPermission(Permissions.AcademicPeriods.Manage)]
    [ProducesResponseType(typeof(ApiResponse<AcademicPeriodDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAcademicPeriod(
        Guid id, [FromBody] UpdateAcademicPeriodRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new UpdateAcademicPeriodCommand(
            id, req.Name, req.StartDate, req.EndDate, req.TermId, req.RowVersion), ct));

    [HttpPost("academic-periods/{id:guid}/set-current")]
    [HasPermission(Permissions.AcademicPeriods.Manage)]
    [ProducesResponseType(typeof(ApiResponse<AcademicPeriodDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCurrentPeriod(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new SetCurrentAcademicPeriodCommand(id), ct));

    [HttpDelete("academic-periods/{id:guid}")]
    [HasPermission(Permissions.AcademicPeriods.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAcademicPeriod(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteAcademicPeriodCommand(id), ct);
        return NoContentResult("Academic period deleted.");
    }

    // ── Education Plans — List & Detail ───────────────────────────────────────

    [HttpGet]
    [HasPermission(Permissions.EducationPlans.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<EducationPlanListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] Guid? academicPeriodId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? guardianVisible = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "desc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetEducationPlansQuery
        {
            CorporationId    = corporationId,
            StudentId        = studentId,
            CampusId         = campusId,
            AcademicPeriodId = academicPeriodId,
            Status           = status,
            GuardianVisible  = guardianVisible,
            Page             = page,
            PageSize         = pageSize,
            Search           = search,
            SortBy           = sortBy,
            SortDirection    = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.EducationPlans.Read)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetEducationPlanQuery(id), ct));

    [HttpGet("guardian-visible")]
    [HasPermission(Permissions.EducationPlans.GuardianView)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuardianVisible(
        [FromQuery] Guid corporationId,
        [FromQuery] Guid studentId,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetGuardianVisiblePlanQuery(corporationId, studentId), ct));

    // ── Plan CRUD ─────────────────────────────────────────────────────────────

    [HttpPost]
    [HasPermission(Permissions.EducationPlans.Create)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEducationPlanRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateEducationPlanCommand(
            req.CorporationId, req.StudentId, req.Title,
            req.AcademicPeriodId, req.CampusId, req.PreparedBy,
            req.EffectiveFrom, req.EffectiveTo), ct);
        return CreatedResult(result, $"/api/education-plans/{result.Id}");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.EducationPlans.Update)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateEducationPlanRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new UpdateEducationPlanCommand(
            id, req.Title, req.AcademicPeriodId, req.CampusId,
            req.PreparedBy, req.EffectiveFrom, req.EffectiveTo, req.RowVersion), ct));

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.EducationPlans.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteEducationPlanCommand(id), ct);
        return NoContentResult("Education plan deleted.");
    }

    // ── Plan Workflow ─────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/submit")]
    [HasPermission(Permissions.EducationPlans.Submit)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new SubmitEducationPlanForReviewCommand(id), ct));

    [HttpPost("{id:guid}/approve")]
    [HasPermission(Permissions.EducationPlans.Approve)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(
        Guid id, [FromBody] ApproveRejectRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new ApproveEducationPlanCommand(id, req.ApproverId, req.Comment), ct));

    [HttpPost("{id:guid}/reject")]
    [HasPermission(Permissions.EducationPlans.Approve)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] ApproveRejectRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new RejectEducationPlanCommand(id, req.ApproverId, req.Comment), ct));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.EducationPlans.Approve)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new ActivateEducationPlanCommand(id), ct));

    [HttpPost("{id:guid}/close")]
    [HasPermission(Permissions.EducationPlans.Update)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new CloseEducationPlanCommand(id), ct));

    [HttpPost("{id:guid}/revise")]
    [HasPermission(Permissions.EducationPlans.Revise)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Revise(
        Guid id, [FromBody] ReviseRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new ReviseEducationPlanCommand(id, req.ChangeSummary), ct));

    [HttpPatch("{id:guid}/guardian-visibility")]
    [HasPermission(Permissions.EducationPlans.Approve)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetGuardianVisibility(
        Guid id, [FromBody] GuardianVisibilityRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new SetGuardianVisibilityCommand(id, req.Visible), ct));

    // ── Plan Goals ────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/goals")]
    [HasPermission(Permissions.EducationPlans.ManageGoals)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddGoal(
        Guid id, [FromBody] AddGoalToPlanRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new AddGoalToPlanCommand(id, req.StudentGoalId, req.Horizon, req.SortOrder), ct));

    [HttpDelete("{id:guid}/goals/{planGoalId:guid}")]
    [HasPermission(Permissions.EducationPlans.ManageGoals)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveGoal(Guid id, Guid planGoalId, CancellationToken ct)
        => OkResult(await Sender.Send(new RemoveGoalFromPlanCommand(id, planGoalId), ct));

    [HttpPut("{id:guid}/goals/reorder")]
    [HasPermission(Permissions.EducationPlans.ManageGoals)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderGoals(
        Guid id, [FromBody] ReorderGoalsRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new ReorderPlanGoalsCommand(id, req.Items), ct));

    // ── Plan Reviews ──────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/reviews")]
    [HasPermission(Permissions.EducationPlans.AddReview)]
    [ProducesResponseType(typeof(ApiResponse<EducationPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddReview(
        Guid id, [FromBody] AddPlanReviewRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new AddPlanReviewCommand(
            id, req.ReviewedOn, req.ReviewerId, req.Summary, req.Outcome), ct));

    // ── Reports ───────────────────────────────────────────────────────────────

    [HttpGet("reports/student-summary")]
    [HasPermission(Permissions.GoalReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<StudentGoalSummaryReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StudentGoalSummaryReport(
        [FromQuery] Guid corporationId,
        [FromQuery] Guid studentId,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetStudentGoalSummaryReportQuery(corporationId, studentId), ct));

    [HttpGet("reports/trend")]
    [HasPermission(Permissions.GoalReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TrendReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TrendReport(
        [FromQuery] Guid corporationId,
        [FromQuery] Guid studentId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetTrendReportQuery(corporationId, studentId, from, to), ct));
}

// ── Request Records ───────────────────────────────────────────────────────────

public record CreateAcademicPeriodRequest(
    Guid CorporationId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TermId,
    bool IsCurrent);

public record UpdateAcademicPeriodRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TermId,
    int RowVersion);

public record CreateEducationPlanRequest(
    Guid CorporationId,
    Guid StudentId,
    string Title,
    Guid? AcademicPeriodId,
    Guid? CampusId,
    Guid? PreparedBy,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo);

public record UpdateEducationPlanRequest(
    string Title,
    Guid? AcademicPeriodId,
    Guid? CampusId,
    Guid? PreparedBy,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    int RowVersion);

public record ApproveRejectRequest(Guid ApproverId, string? Comment);

public record ReviseRequest(string? ChangeSummary);

public record GuardianVisibilityRequest(bool Visible);

public record AddGoalToPlanRequest(Guid StudentGoalId, string Horizon, int SortOrder);

public record ReorderGoalsRequest(IReadOnlyList<PlanGoalOrderItem> Items);

public record AddPlanReviewRequest(
    DateOnly ReviewedOn,
    Guid? ReviewerId,
    string? Summary,
    string? Outcome);
