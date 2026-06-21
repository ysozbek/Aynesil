using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Goals.Commands;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Application.Features.Goals.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Goal Library, Goal Templates, Student Goals, Goal Progress, and Goal Analytics.
/// Route: /api/goals
/// </summary>
[Route("api/goals")]
public sealed class GoalsController : BaseController
{
    // ── Goal Libraries ────────────────────────────────────────────────────────

    [HttpGet("libraries")]
    [HasPermission(Permissions.GoalLibraries.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GoalLibraryListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLibraries(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetGoalLibrariesQuery
        {
            CorporationId = corporationId,
            Page          = page,
            PageSize      = pageSize,
            Search        = search,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("libraries/{id:guid}")]
    [HasPermission(Permissions.GoalLibraries.Read)]
    [ProducesResponseType(typeof(ApiResponse<GoalLibraryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLibrary(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetGoalLibraryQuery(id), ct));

    [HttpPost("libraries")]
    [HasPermission(Permissions.GoalLibraries.Create)]
    [ProducesResponseType(typeof(ApiResponse<GoalLibraryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLibrary(
        [FromBody] CreateGoalLibraryRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new CreateGoalLibraryCommand(req.CorporationId, req.Name, req.Description), ct);
        return CreatedResult(result, $"/api/goals/libraries/{result.Id}");
    }

    [HttpPut("libraries/{id:guid}")]
    [HasPermission(Permissions.GoalLibraries.Update)]
    [ProducesResponseType(typeof(ApiResponse<GoalLibraryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLibrary(
        Guid id, [FromBody] UpdateGoalLibraryRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new UpdateGoalLibraryCommand(id, req.Name, req.Description, req.RowVersion), ct));

    [HttpDelete("libraries/{id:guid}")]
    [HasPermission(Permissions.GoalLibraries.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteLibrary(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteGoalLibraryCommand(id), ct);
        return NoContentResult("Goal library deleted.");
    }

    // ── Goal Templates ────────────────────────────────────────────────────────

    [HttpGet("templates")]
    [HasPermission(Permissions.GoalTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GoalTemplateListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? libraryId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? developmentAreaId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetGoalTemplatesQuery
        {
            CorporationId   = corporationId,
            LibraryId       = libraryId,
            CategoryId      = categoryId,
            DevelopmentAreaId = developmentAreaId,
            Page            = page,
            PageSize        = pageSize,
            Search          = search,
            SortBy          = sortBy,
            SortDirection   = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("templates/{id:guid}")]
    [HasPermission(Permissions.GoalTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<GoalTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplate(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetGoalTemplateQuery(id), ct));

    [HttpPost("templates")]
    [HasPermission(Permissions.GoalTemplates.Create)]
    [ProducesResponseType(typeof(ApiResponse<GoalTemplateDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateGoalTemplateRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateGoalTemplateCommand(
            req.CorporationId, req.LibraryId, req.CategoryId,
            req.DevelopmentAreaId, req.Code, req.Statement, req.DefaultCriteria), ct);
        return CreatedResult(result, $"/api/goals/templates/{result.Id}");
    }

    [HttpPut("templates/{id:guid}")]
    [HasPermission(Permissions.GoalTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse<GoalTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTemplate(
        Guid id, [FromBody] UpdateGoalTemplateRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new UpdateGoalTemplateCommand(
            id, req.LibraryId, req.CategoryId, req.DevelopmentAreaId,
            req.Code, req.Statement, req.DefaultCriteria, req.RowVersion), ct));

    [HttpPut("templates/{id:guid}/translations/{locale}")]
    [HasPermission(Permissions.GoalTemplates.Translate)]
    [ProducesResponseType(typeof(ApiResponse<GoalTemplateTranslationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetTranslation(
        Guid id, string locale, [FromBody] SetGoalTemplateTranslationRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new SetGoalTemplateTranslationCommand(id, locale, req.Statement, req.DefaultCriteria), ct));

    [HttpDelete("templates/{id:guid}")]
    [HasPermission(Permissions.GoalTemplates.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteGoalTemplateCommand(id), ct);
        return NoContentResult("Goal template deleted.");
    }

    // ── Student Goals ─────────────────────────────────────────────────────────

    [HttpGet("student-goals")]
    [HasPermission(Permissions.StudentGoals.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<StudentGoalListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentGoals(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] string? horizon = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? developmentAreaId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetStudentGoalsQuery
        {
            CorporationId   = corporationId,
            StudentId       = studentId,
            Horizon         = horizon,
            Status          = status,
            CategoryId      = categoryId,
            DevelopmentAreaId = developmentAreaId,
            Page            = page,
            PageSize        = pageSize,
            Search          = search,
            SortBy          = sortBy,
            SortDirection   = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("student-goals/{id:guid}")]
    [HasPermission(Permissions.StudentGoals.Read)]
    [ProducesResponseType(typeof(ApiResponse<StudentGoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentGoal(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetStudentGoalQuery(id), ct));

    [HttpPost("student-goals")]
    [HasPermission(Permissions.StudentGoals.Create)]
    [ProducesResponseType(typeof(ApiResponse<StudentGoalDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStudentGoal(
        [FromBody] CreateStudentGoalRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateStudentGoalCommand(
            req.CorporationId, req.StudentId, req.Statement, req.Horizon,
            req.TemplateId, req.CategoryId, req.DevelopmentAreaId,
            req.ParentGoalId, req.MasteryCriteria, req.Baseline,
            req.TargetValue, req.StartDate, req.TargetDate), ct);
        return CreatedResult(result, $"/api/goals/student-goals/{result.Id}");
    }

    [HttpPut("student-goals/{id:guid}")]
    [HasPermission(Permissions.StudentGoals.Update)]
    [ProducesResponseType(typeof(ApiResponse<StudentGoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStudentGoal(
        Guid id, [FromBody] UpdateStudentGoalRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new UpdateStudentGoalCommand(
            id, req.Statement, req.CategoryId, req.DevelopmentAreaId,
            req.MasteryCriteria, req.Baseline, req.TargetValue,
            req.StartDate, req.TargetDate, req.RowVersion), ct));

    [HttpPost("student-goals/{id:guid}/status")]
    [HasPermission(Permissions.StudentGoals.ChangeStatus)]
    [ProducesResponseType(typeof(ApiResponse<StudentGoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeGoalStatus(
        Guid id, [FromBody] ChangeGoalStatusRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new ChangeStudentGoalStatusCommand(id, req.NewStatus, req.AchievedDate), ct));

    [HttpDelete("student-goals/{id:guid}")]
    [HasPermission(Permissions.StudentGoals.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteStudentGoal(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteStudentGoalCommand(id), ct);
        return NoContentResult("Student goal deleted.");
    }

    // ── Goal Progress ─────────────────────────────────────────────────────────

    [HttpGet("student-goals/{goalId:guid}/progress")]
    [HasPermission(Permissions.GoalProgress.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GoalProgressDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgress(
        Guid goalId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(new GetGoalProgressQuery(goalId, from, to), ct));

    [HttpGet("student-goals/{goalId:guid}/trend")]
    [HasPermission(Permissions.GoalProgress.Read)]
    [ProducesResponseType(typeof(ApiResponse<GoalTrendDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrend(Guid goalId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetGoalTrendQuery(goalId), ct));

    [HttpPost("student-goals/{goalId:guid}/progress")]
    [HasPermission(Permissions.GoalProgress.Record)]
    [ProducesResponseType(typeof(ApiResponse<GoalProgressDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordProgress(
        Guid goalId, [FromBody] RecordProgressRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new RecordGoalProgressCommand(
            goalId, req.MeasuredOn, req.MeasuredValue, req.PercentComplete,
            req.Trend, req.Note, req.SessionId), ct);
        return CreatedResult(result, $"/api/goals/student-goals/{goalId}/progress/{result.Id}");
    }

    // ── Analytics ─────────────────────────────────────────────────────────────

    [HttpGet("analytics/student-summary")]
    [HasPermission(Permissions.GoalReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<StudentGoalSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentSummary(
        [FromQuery] Guid corporationId,
        [FromQuery] Guid studentId,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetStudentGoalSummaryQuery(corporationId, studentId), ct));

    [HttpGet("analytics/success-rates")]
    [HasPermission(Permissions.GoalReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GoalSuccessRateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuccessRates(
        [FromQuery] Guid corporationId,
        [FromQuery] Guid? campusId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetGoalSuccessRatesQuery(corporationId, campusId, from, to), ct));
}

// ── Request Records ───────────────────────────────────────────────────────────

public record CreateGoalLibraryRequest(Guid? CorporationId, string Name, string? Description);
public record UpdateGoalLibraryRequest(string Name, string? Description, int RowVersion);

public record CreateGoalTemplateRequest(
    Guid? CorporationId,
    Guid? LibraryId,
    Guid? CategoryId,
    Guid? DevelopmentAreaId,
    string? Code,
    string Statement,
    string? DefaultCriteria);

public record UpdateGoalTemplateRequest(
    Guid? LibraryId,
    Guid? CategoryId,
    Guid? DevelopmentAreaId,
    string? Code,
    string Statement,
    string? DefaultCriteria,
    int RowVersion);

public record SetGoalTemplateTranslationRequest(string Statement, string? DefaultCriteria);

public record CreateStudentGoalRequest(
    Guid CorporationId,
    Guid StudentId,
    string Statement,
    string Horizon,
    Guid? TemplateId,
    Guid? CategoryId,
    Guid? DevelopmentAreaId,
    Guid? ParentGoalId,
    string? MasteryCriteria,
    string? Baseline,
    decimal? TargetValue,
    DateOnly? StartDate,
    DateOnly? TargetDate);

public record UpdateStudentGoalRequest(
    string Statement,
    Guid? CategoryId,
    Guid? DevelopmentAreaId,
    string? MasteryCriteria,
    string? Baseline,
    decimal? TargetValue,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    int RowVersion);

public record ChangeGoalStatusRequest(string NewStatus, DateOnly? AchievedDate);

public record RecordProgressRequest(
    DateOnly MeasuredOn,
    decimal? MeasuredValue,
    decimal? PercentComplete,
    string? Trend,
    string? Note,
    Guid? SessionId);
