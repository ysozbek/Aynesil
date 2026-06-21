using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Programs.Commands;
using Aynesil.Application.Features.Programs.Dtos;
using Aynesil.Application.Features.Programs.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Program catalog, enrollment, and student-program assignment management.
/// Route: /api/programs
/// </summary>
[Route("api/programs")]
public sealed class ProgramsController : BaseController
{
    // ── Program List &amp; Detail ─────────────────────────────────────────────────

    [HttpGet]
    [HasPermission(Permissions.Programs.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProgramListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? programTypeId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetProgramsQuery
        {
            CorporationId = corporationId,
            ProgramTypeId = programTypeId,
            IsActive      = isActive,
            Page          = page,
            PageSize      = pageSize,
            Search        = search,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Programs.Read)]
    [ProducesResponseType(typeof(ApiResponse<ProgramDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetProgramQuery(id), ct);
        return OkResult(result);
    }

    // ── Program CRUD ──────────────────────────────────────────────────────────

    [HttpPost]
    [HasPermission(Permissions.Programs.Create)]
    [ProducesResponseType(typeof(ApiResponse<ProgramDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateProgramRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateProgramCommand(
            req.CorporationId, req.Code, req.Name, req.ProgramTypeId, req.Description), ct);
        return CreatedResult(result, $"/api/programs/{result.Id}");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Programs.Update)]
    [ProducesResponseType(typeof(ApiResponse<ProgramDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProgramRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateProgramCommand(
            id, req.Code, req.Name, req.ProgramTypeId, req.Description, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Programs.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteProgramCommand(id), ct);
        return NoContentResult("Program deleted.");
    }

    // ── Program Translation Workflow ──────────────────────────────────────────

    [HttpPut("{id:guid}/translations/{locale}")]
    [HasPermission(Permissions.Programs.Update)]
    [ProducesResponseType(typeof(ApiResponse<ProgramTranslationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetTranslation(
        Guid id, string locale, [FromBody] SetTranslationRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new SetProgramTranslationCommand(id, locale, req.Name, req.Description), ct);
        return OkResult(result);
    }

    // ── Program Service Workflow (Program Assignment) ─────────────────────────

    [HttpPost("{id:guid}/services")]
    [HasPermission(Permissions.Programs.Update)]
    [ProducesResponseType(typeof(ApiResponse<ProgramServiceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddService(
        Guid id, [FromBody] AddProgramServiceRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AddProgramServiceCommand(
            id, req.Name, req.ServiceTypeId,
            req.DefaultDurationMinutes, req.DefaultSessionsPerWeek, req.SortOrder), ct);
        return CreatedResult(result, $"/api/programs/{id}/services/{result.Id}");
    }

    [HttpPut("{id:guid}/services/{serviceId:guid}")]
    [HasPermission(Permissions.Programs.Update)]
    [ProducesResponseType(typeof(ApiResponse<ProgramServiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateService(
        Guid id, Guid serviceId, [FromBody] UpdateProgramServiceRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateProgramServiceCommand(
            serviceId, req.Name, req.ServiceTypeId,
            req.DefaultDurationMinutes, req.DefaultSessionsPerWeek, req.SortOrder), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}/services/{serviceId:guid}")]
    [HasPermission(Permissions.Programs.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteService(Guid id, Guid serviceId, CancellationToken ct)
    {
        await Sender.Send(new DeleteProgramServiceCommand(serviceId), ct);
        return NoContentResult("Program service deleted.");
    }
}

// ── Enrollments sub-controller ────────────────────────────────────────────────

/// <summary>
/// Student enrollment management.
/// Route: /api/enrollments
/// </summary>
[Route("api/enrollments")]
public sealed class EnrollmentsController : BaseController
{
    [HttpGet]
    [HasPermission(Permissions.Enrollments.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<EnrollmentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] Guid? statusId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetStudentEnrollmentsQuery
        {
            StudentId     = studentId,
            CorporationId = corporationId,
            CampusId      = campusId,
            StatusId      = statusId,
            IsActive      = isActive,
            Page          = page,
            PageSize      = pageSize,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Enrollments.Read)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetEnrollmentQuery(id), ct);
        return OkResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Enrollments.Create)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateEnrollmentCommand(
            req.CorporationId, req.StudentId, req.CampusId, req.StatusId, req.EnrolledOn), ct);
        return CreatedResult(result, $"/api/enrollments/{result.Id}");
    }

    [HttpPost("{id:guid}/status")]
    [HasPermission(Permissions.Enrollments.Update)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStatus(
        Guid id, [FromBody] ChangeEnrollmentStatusRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new ChangeEnrollmentStatusCommand(id, req.NewStatusId, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpPost("{id:guid}/end")]
    [HasPermission(Permissions.Enrollments.Update)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> End(Guid id, [FromBody] EndProgramEnrollmentRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new EndEnrollmentCommand(id, req.EndedOn, req.TerminationReason, req.RowVersion), ct);
        return OkResult(result);
    }

    // ── Student Program Assignment Workflow ───────────────────────────────────

    [HttpPost("{id:guid}/programs")]
    [HasPermission(Permissions.Enrollments.ManagePrograms)]
    [ProducesResponseType(typeof(ApiResponse<StudentProgramDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignProgram(
        Guid id, [FromBody] AssignStudentToProgramRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AssignStudentToProgramCommand(
            req.CorporationId, req.StudentId, req.ProgramId,
            id, req.CampusId, req.StartDate, req.EndDate), ct);
        return CreatedResult(result, $"/api/enrollments/{id}/programs/{result.Id}");
    }

    [HttpPut("{id:guid}/programs/{spId:guid}")]
    [HasPermission(Permissions.Enrollments.ManagePrograms)]
    [ProducesResponseType(typeof(ApiResponse<StudentProgramDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStudentProgram(
        Guid id, Guid spId, [FromBody] UpdateStudentProgramRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new UpdateStudentProgramCommand(spId, req.StartDate, req.EndDate, req.Status, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}/programs/{spId:guid}")]
    [HasPermission(Permissions.Enrollments.ManagePrograms)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveStudentProgram(Guid id, Guid spId, CancellationToken ct)
    {
        await Sender.Send(new RemoveStudentFromProgramCommand(spId), ct);
        return NoContentResult("Student program assignment removed.");
    }
}

// ── Student Programs standalone query ─────────────────────────────────────────

/// <summary>
/// Student-program assignment queries (standalone, not nested under enrollment).
/// Route: /api/student-programs
/// </summary>
[Route("api/student-programs")]
public sealed class StudentProgramsController : BaseController
{
    [HttpGet]
    [HasPermission(Permissions.Enrollments.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<StudentProgramListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? programId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? enrollmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetStudentProgramsQuery
        {
            StudentId     = studentId,
            CorporationId = corporationId,
            ProgramId     = programId,
            CampusId      = campusId,
            Status        = status,
            EnrollmentId  = enrollmentId,
            Page          = page,
            PageSize      = pageSize,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Enrollments.Read)]
    [ProducesResponseType(typeof(ApiResponse<StudentProgramDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetStudentProgramQuery(id), ct);
        return OkResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Enrollments.ManagePrograms)]
    [ProducesResponseType(typeof(ApiResponse<StudentProgramDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Assign([FromBody] AssignStudentToProgramRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AssignStudentToProgramCommand(
            req.CorporationId, req.StudentId, req.ProgramId,
            req.EnrollmentId, req.CampusId, req.StartDate, req.EndDate), ct);
        return CreatedResult(result, $"/api/student-programs/{result.Id}");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateProgramRequest(
    Guid CorporationId,
    string Code,
    string Name,
    Guid? ProgramTypeId,
    string? Description);

public record UpdateProgramRequest(
    string Code,
    string Name,
    Guid? ProgramTypeId,
    string? Description,
    int RowVersion);

public record SetTranslationRequest(string Name, string? Description);

public record AddProgramServiceRequest(
    string Name,
    Guid? ServiceTypeId,
    int? DefaultDurationMinutes,
    decimal? DefaultSessionsPerWeek,
    int SortOrder = 0);

public record UpdateProgramServiceRequest(
    string Name,
    Guid? ServiceTypeId,
    int? DefaultDurationMinutes,
    decimal? DefaultSessionsPerWeek,
    int SortOrder);

public record CreateEnrollmentRequest(
    Guid CorporationId,
    Guid StudentId,
    Guid? CampusId,
    Guid? StatusId,
    DateOnly? EnrolledOn);

public record ChangeEnrollmentStatusRequest(Guid NewStatusId, int RowVersion);

public record EndProgramEnrollmentRequest(DateOnly? EndedOn, string? TerminationReason, int RowVersion);

public record AssignStudentToProgramRequest(
    Guid CorporationId,
    Guid StudentId,
    Guid ProgramId,
    Guid? EnrollmentId,
    Guid? CampusId,
    DateOnly? StartDate,
    DateOnly? EndDate);

public record UpdateStudentProgramRequest(
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Status,
    int RowVersion);
