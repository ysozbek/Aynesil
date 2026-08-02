using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Consultancy.Commands;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Application.Features.Consultancy.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// School Consultancy Management: institutions, plans, visits, observations, reports.
/// Route: /api/consultancy
/// </summary>
[Route("api/consultancy")]
public sealed class ConsultancyController : BaseController
{
    // ═══════════════════════════════════════════════════════════════════════════
    // INSTITUTIONS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Paginated list of institutions. Filterable by corporation, type, city.</summary>
    [HttpGet("institutions")]
    [HasPermission(Permissions.Institutions.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<InstitutionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInstitutions(
        [FromQuery] GetInstitutionsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full institution detail.</summary>
    [HttpGet("institutions/{id:guid}")]
    [HasPermission(Permissions.Institutions.Read)]
    [ProducesResponseType(typeof(ApiResponse<InstitutionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInstitution(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetInstitutionQuery(id), ct));

    /// <summary>Create a new institution.</summary>
    [HttpPost("institutions")]
    [HasPermission(Permissions.Institutions.Create)]
    [ProducesResponseType(typeof(ApiResponse<InstitutionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInstitution(
        [FromBody] CreateInstitutionCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/consultancy/institutions/{result.Id}");
    }

    /// <summary>Update institution details (name, type, contact info).</summary>
    [HttpPut("institutions/{id:guid}")]
    [HasPermission(Permissions.Institutions.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateInstitution(
        Guid id, [FromBody] UpdateInstitutionCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Soft-delete an institution. Fails if it has active consultancy plans.</summary>
    [HttpDelete("institutions/{id:guid}")]
    [HasPermission(Permissions.Institutions.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteInstitution(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteInstitutionCommand(id), ct);
        return NoContentResult();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CONSULTANCY PLANS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Paginated list of consultancy plans. Filterable by institution, status, type, educator.</summary>
    [HttpGet("plans")]
    [HasPermission(Permissions.ConsultancyPlans.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ConsultancyPlanListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(
        [FromQuery] GetConsultancyPlansQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full consultancy plan detail.</summary>
    [HttpGet("plans/{id:guid}")]
    [HasPermission(Permissions.ConsultancyPlans.Read)]
    [ProducesResponseType(typeof(ApiResponse<ConsultancyPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlan(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetConsultancyPlanQuery(id), ct));

    /// <summary>Create a new consultancy plan for an institution (starts in draft).</summary>
    [HttpPost("plans")]
    [HasPermission(Permissions.ConsultancyPlans.Create)]
    [ProducesResponseType(typeof(ApiResponse<ConsultancyPlanDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlan(
        [FromBody] CreateConsultancyPlanCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/consultancy/plans/{result.Id}");
    }

    /// <summary>Update plan details. Only allowed for draft or active plans.</summary>
    [HttpPut("plans/{id:guid}")]
    [HasPermission(Permissions.ConsultancyPlans.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePlan(
        Guid id, [FromBody] UpdateConsultancyPlanCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Activate a consultancy plan (draft → active).</summary>
    [HttpPost("plans/{id:guid}/activate")]
    [HasPermission(Permissions.ConsultancyPlans.Activate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivatePlan(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ActivateConsultancyPlanCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Mark a plan as completed (active → completed).</summary>
    [HttpPost("plans/{id:guid}/complete")]
    [HasPermission(Permissions.ConsultancyPlans.Complete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompletePlan(Guid id, CancellationToken ct)
    {
        await Sender.Send(new CompleteConsultancyPlanCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Cancel a consultancy plan (draft|active → cancelled).</summary>
    [HttpPost("plans/{id:guid}/cancel")]
    [HasPermission(Permissions.ConsultancyPlans.Cancel)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelPlan(Guid id, CancellationToken ct)
    {
        await Sender.Send(new CancelConsultancyPlanCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Hard-delete a draft plan with no visits.</summary>
    [HttpDelete("plans/{id:guid}")]
    [HasPermission(Permissions.ConsultancyPlans.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteConsultancyPlanCommand(id), ct);
        return NoContentResult();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SCHOOL VISITS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Paginated visit list. Filterable by institution, plan, visitor, status, date range.</summary>
    [HttpGet("visits")]
    [HasPermission(Permissions.SchoolVisits.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<SchoolVisitListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVisits(
        [FromQuery] GetSchoolVisitsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full visit detail including all observations.</summary>
    [HttpGet("visits/{id:guid}")]
    [HasPermission(Permissions.SchoolVisits.Read)]
    [ProducesResponseType(typeof(ApiResponse<SchoolVisitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVisit(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetSchoolVisitQuery(id), ct));

    /// <summary>Schedule a new school visit.</summary>
    [HttpPost("visits")]
    [HasPermission(Permissions.SchoolVisits.Create)]
    [ProducesResponseType(typeof(ApiResponse<SchoolVisitDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleVisit(
        [FromBody] ScheduleSchoolVisitCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/consultancy/visits/{result.Id}");
    }

    /// <summary>Update visit details (date, visitor, purpose, plan link). Only planned visits.</summary>
    [HttpPut("visits/{id:guid}")]
    [HasPermission(Permissions.SchoolVisits.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateVisit(
        Guid id, [FromBody] UpdateSchoolVisitCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Mark a visit as completed (planned → completed).</summary>
    [HttpPost("visits/{id:guid}/complete")]
    [HasPermission(Permissions.SchoolVisits.Complete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteVisit(Guid id, CancellationToken ct)
    {
        await Sender.Send(new CompleteSchoolVisitCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Cancel a planned visit (planned → cancelled).</summary>
    [HttpPost("visits/{id:guid}/cancel")]
    [HasPermission(Permissions.SchoolVisits.Cancel)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelVisit(Guid id, CancellationToken ct)
    {
        await Sender.Send(new CancelSchoolVisitCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Hard-delete a planned visit with no observations.</summary>
    [HttpDelete("visits/{id:guid}")]
    [HasPermission(Permissions.SchoolVisits.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteVisit(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteSchoolVisitCommand(id), ct);
        return NoContentResult();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OBSERVATIONS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>List all observations for a visit.</summary>
    [HttpGet("visits/{visitId:guid}/observations")]
    [HasPermission(Permissions.Observations.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ObservationRecordDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetObservations(Guid visitId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetObservationsQuery(visitId), ct));

    /// <summary>Get a single observation record.</summary>
    [HttpGet("observations/{id:guid}")]
    [HasPermission(Permissions.Observations.Read)]
    [ProducesResponseType(typeof(ApiResponse<ObservationRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetObservation(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetObservationQuery(id), ct));

    /// <summary>Record an observation during a visit.</summary>
    [HttpPost("visits/{visitId:guid}/observations")]
    [HasPermission(Permissions.Observations.Create)]
    [ProducesResponseType(typeof(ApiResponse<ObservationRecordDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateObservation(
        Guid visitId, [FromBody] CreateObservationCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { SchoolVisitId = visitId }, ct);
        return CreatedResult(result, $"/api/consultancy/observations/{result.Id}");
    }

    /// <summary>Update an observation (text, type, subject, recommendations).</summary>
    [HttpPut("observations/{id:guid}")]
    [HasPermission(Permissions.Observations.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateObservation(
        Guid id, [FromBody] UpdateObservationCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Delete an observation record.</summary>
    [HttpDelete("observations/{id:guid}")]
    [HasPermission(Permissions.Observations.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteObservation(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteObservationCommand(id), ct);
        return NoContentResult();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CONSULTANCY REPORTS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Paginated report list. Filterable by plan, visit, corporation.</summary>
    [HttpGet("reports")]
    [HasPermission(Permissions.ConsultancyReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ConsultancyReportListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports(
        [FromQuery] GetConsultancyReportsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full consultancy report detail.</summary>
    [HttpGet("reports/{id:guid}")]
    [HasPermission(Permissions.ConsultancyReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<ConsultancyReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetConsultancyReportQuery(id), ct));

    /// <summary>Create a consultancy report (must be linked to a plan, visit, or both).</summary>
    [HttpPost("reports")]
    [HasPermission(Permissions.ConsultancyReports.Create)]
    [ProducesResponseType(typeof(ApiResponse<ConsultancyReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReport(
        [FromBody] CreateConsultancyReportCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/consultancy/reports/{result.Id}");
    }

    /// <summary>Hard-delete a consultancy report.</summary>
    [HttpDelete("reports/{id:guid}")]
    [HasPermission(Permissions.ConsultancyReports.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReport(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteConsultancyReportCommand(id), ct);
        return NoContentResult();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REPORTING
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Institution Report: activity summary per institution
    /// (plan counts, visit counts, observation counts, report counts).
    /// Filterable by corporation and institution type.
    /// </summary>
    [HttpGet("reporting/institutions")]
    [HasPermission(Permissions.Institutions.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InstitutionReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInstitutionReport(
        [FromQuery] GetInstitutionReportQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>
    /// Consultancy Outcomes Report: plan-level outcomes with visit, observation and report counts.
    /// Filterable by institution and status.
    /// </summary>
    [HttpGet("reporting/outcomes")]
    [HasPermission(Permissions.ConsultancyPlans.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConsultancyOutcomesDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsultancyOutcomes(
        [FromQuery] GetConsultancyOutcomesQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>
    /// Visit History Report: chronological visit log with observation and report counts.
    /// Filterable by institution and date range. Used for calendar views and audit.
    /// </summary>
    [HttpGet("reporting/visit-history")]
    [HasPermission(Permissions.SchoolVisits.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VisitHistoryItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVisitHistory(
        [FromQuery] GetVisitHistoryQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));
}
