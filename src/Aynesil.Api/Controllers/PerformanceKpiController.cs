using Aynesil.Api.Authorization;
using Aynesil.Application.Features.PerformanceKpi.Commands;
using Aynesil.Application.Features.PerformanceKpi.Dtos;
using Aynesil.Application.Features.PerformanceKpi.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Educator Performance &amp; KPI:
///   KPI definitions · KPI values · Performance snapshots (KPI engine) ·
///   Parent feedback · Educator / Manager / Executive dashboards ·
///   Trend analysis · Ranking · Reports
/// Route: /api/performance-kpi
/// </summary>
[Route("api/performance-kpi")]
public sealed class PerformanceKpiController : BaseController
{
    // ── KPI Categories ────────────────────────────────────────────────────────

    /// <summary>List all active KPI categories (session_performance, attendance_performance, …).</summary>
    [HttpGet("categories")]
    [HasPermission(Permissions.Kpi.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<KpiCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] Guid? corporationId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetKpiCategoriesQuery(corporationId), ct));

    // ── KPI Definitions ───────────────────────────────────────────────────────

    /// <summary>
    /// Paginated KPI definition list.
    /// Set includePlatform=true (default) to include the six built-in platform KPIs
    /// alongside any tenant-custom definitions.
    /// </summary>
    [HttpGet("definitions")]
    [HasPermission(Permissions.Kpi.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<KpiDefinitionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefinitions(
        [FromQuery] GetKpiDefinitionsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Get a single KPI definition by ID (includes spec JSON).</summary>
    [HttpGet("definitions/{id:guid}")]
    [HasPermission(Permissions.Kpi.Read)]
    [ProducesResponseType(typeof(ApiResponse<KpiDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDefinition(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetKpiDefinitionQuery(id), ct));

    /// <summary>Create a tenant-custom KPI definition.</summary>
    [HttpPost("definitions")]
    [HasPermission(Permissions.Kpi.Manage)]
    [ProducesResponseType(typeof(ApiResponse<KpiDefinitionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDefinition(
        [FromBody] CreateKpiDefinitionCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/performance-kpi/definitions/{result.Id}");
    }

    /// <summary>Update a KPI definition (name, category, unit, spec formula).</summary>
    [HttpPut("definitions/{id:guid}")]
    [HasPermission(Permissions.Kpi.Manage)]
    [ProducesResponseType(typeof(ApiResponse<KpiDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDefinition(
        Guid id, [FromBody] UpdateKpiDefinitionCommand command, CancellationToken ct)
        => OkResult(await Sender.Send(command with { Id = id }, ct));

    /// <summary>Activate a KPI definition (inactive → active).</summary>
    [HttpPost("definitions/{id:guid}/activate")]
    [HasPermission(Permissions.Kpi.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateDefinition(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ActivateKpiDefinitionCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Deactivate a KPI definition (active → inactive). Excludes it from computation runs.</summary>
    [HttpPost("definitions/{id:guid}/deactivate")]
    [HasPermission(Permissions.Kpi.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateDefinition(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeactivateKpiDefinitionCommand(id), ct);
        return NoContentResult();
    }

    // ── KPI Values ────────────────────────────────────────────────────────────

    /// <summary>
    /// All computed KPI values for a single educator.
    /// Optionally filter by period and KPI code prefix.
    /// </summary>
    [HttpGet("educators/{educatorId:guid}/kpi-values")]
    [HasPermission(Permissions.KpiSnapshots.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<KpiValueDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEducatorKpiValues(
        Guid educatorId, [FromQuery] GetEducatorKpiValuesQuery query, CancellationToken ct)
    {
        query.EducatorId = educatorId;
        return OkResult(await Sender.Send(query, ct));
    }

    /// <summary>Generic paginated KPI value list. Supports all subject types and periods.</summary>
    [HttpGet("kpi-values")]
    [HasPermission(Permissions.KpiSnapshots.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<KpiValueDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKpiValues(
        [FromQuery] GetKpiValuesQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    // ── KPI Computation Engine ────────────────────────────────────────────────

    /// <summary>
    /// Compute (or refresh) the KPI snapshot for one educator and period.
    /// Writes to both educator_performance_snapshot and core.kpi_value.
    /// Idempotent — calling again with the same inputs overwrites the previous result.
    /// </summary>
    [HttpPost("educators/{educatorId:guid}/compute")]
    [HasPermission(Permissions.Kpi.Compute)]
    [ProducesResponseType(typeof(ApiResponse<EducatorPerformanceSnapshotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ComputeSnapshot(
        Guid educatorId,
        [FromBody] ComputePerformanceSnapshotCommand command,
        CancellationToken ct)
        => OkResult(await Sender.Send(command with { EducatorId = educatorId }, ct));

    /// <summary>
    /// Bulk-compute KPI snapshots for ALL active educators in a corporation for a given period.
    /// Returns the count of successfully computed snapshots. Individual failures are non-fatal.
    /// </summary>
    [HttpPost("compute/bulk")]
    [HasPermission(Permissions.Kpi.Compute)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkComputeSnapshots(
        [FromBody] BulkComputeSnapshotsCommand command, CancellationToken ct)
        => OkResult(await Sender.Send(command, ct));

    // ── Performance Snapshots ─────────────────────────────────────────────────

    /// <summary>
    /// Paginated performance snapshot list. Filterable by educator, campus, and period range.
    /// Primary use: Performance Report screen.
    /// </summary>
    [HttpGet("snapshots")]
    [HasPermission(Permissions.KpiSnapshots.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<EducatorPerformanceSnapshotListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnapshots(
        [FromQuery] GetPerformanceSnapshotsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    // ── Parent Feedback ───────────────────────────────────────────────────────

    /// <summary>Paginated parent feedback list. Filterable by educator, session, guardian, rating, date range.</summary>
    [HttpGet("parent-feedback")]
    [HasPermission(Permissions.ParentFeedback.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ParentFeedbackDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParentFeedback(
        [FromQuery] GetParentFeedbackQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>
    /// Submit a parent feedback rating for a session.
    /// Validates educator and session belong to the same corporation.
    /// </summary>
    [HttpPost("parent-feedback")]
    [HasPermission(Permissions.ParentFeedback.Submit)]
    [ProducesResponseType(typeof(ApiResponse<ParentFeedbackDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitFeedback(
        [FromBody] SubmitParentFeedbackCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/performance-kpi/parent-feedback/{result.Id}");
    }

    // ── Dashboards ────────────────────────────────────────────────────────────

    /// <summary>
    /// Educator self-service dashboard: current &amp; previous period summaries,
    /// all KPI values, 6-period session + attendance trends, and 10 recent feedback entries.
    /// Reads pre-computed snapshots — requires ComputeSnapshot to have been called first.
    /// </summary>
    [HttpGet("dashboards/educator/{educatorId:guid}")]
    [HasPermission(Permissions.KpiDashboard.Read)]
    [ProducesResponseType(typeof(ApiResponse<EducatorDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEducatorDashboard(
        Guid educatorId, [FromQuery] GetEducatorDashboardQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query with { EducatorId = educatorId }, ct));

    /// <summary>
    /// Manager dashboard: aggregated corporation (or campus) averages,
    /// all educator summaries ranked by attendance rate, and top-5 performers.
    /// </summary>
    [HttpGet("dashboards/manager")]
    [HasPermission(Permissions.KpiDashboard.Read)]
    [ProducesResponseType(typeof(ApiResponse<ManagerDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetManagerDashboard(
        [FromQuery] GetManagerDashboardQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>
    /// Executive dashboard: corporation-wide aggregates, 12-period multi-KPI trends,
    /// total session volume, and top-10 performers.
    /// </summary>
    [HttpGet("dashboards/executive")]
    [HasPermission(Permissions.KpiDashboard.Read)]
    [ProducesResponseType(typeof(ApiResponse<ExecutiveDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutiveDashboard(
        [FromQuery] GetExecutiveDashboardQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    // ── Trend Analysis ────────────────────────────────────────────────────────

    /// <summary>KPI trend for a single educator across up to 12 periods (oldest→newest).</summary>
    [HttpGet("trends/educator/{educatorId:guid}")]
    [HasPermission(Permissions.KpiReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<KpiTrendDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEducatorTrend(
        Guid educatorId, [FromQuery] GetEducatorKpiTrendQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query with { EducatorId = educatorId }, ct));

    /// <summary>Corporation-wide average KPI trend across up to 12 periods.</summary>
    [HttpGet("trends/corporation")]
    [HasPermission(Permissions.KpiReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<KpiTrendDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCorporationTrend(
        [FromQuery] GetCorporationKpiTrendQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    // ── Ranking ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Educator ranking by a specific KPI for a period.
    /// Supports campus scope and ascending/descending rank order.
    /// </summary>
    [HttpGet("ranking")]
    [HasPermission(Permissions.KpiReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RankingItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRanking(
        [FromQuery] GetEducatorRankingQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    // ── Reports ───────────────────────────────────────────────────────────────

    /// <summary>
    /// KPI Performance Report: all educators' metrics for a single period
    /// with ordinal rank by attendance rate. Suitable for export.
    /// </summary>
    [HttpGet("reports/kpi")]
    [HasPermission(Permissions.KpiReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<KpiReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKpiReport(
        [FromQuery] GetKpiReportQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));
}
