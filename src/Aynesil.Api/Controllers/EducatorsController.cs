using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Educators.Commands;
using Aynesil.Application.Features.Educators.Dtos;
using Aynesil.Application.Features.Educators.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Educator lifecycle and profile management.
/// Includes specialty, campus, certification, and hierarchy sub-resources.
/// Route: /api/educators
/// </summary>
[Route("api/educators")]
public sealed class EducatorsController : BaseController
{
    // ── Educator List &amp; Detail ─────────────────────────────────────────────────

    [HttpGet]
    [HasPermission(Permissions.Educators.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<EducatorListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] Guid? titleId = null,
        [FromQuery] Guid? specialtyId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? employmentType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetEducatorsQuery
        {
            CorporationId  = corporationId,
            CampusId       = campusId,
            TitleId        = titleId,
            SpecialtyId    = specialtyId,
            IsActive       = isActive,
            EmploymentType = employmentType,
            Page           = page,
            PageSize       = pageSize,
            Search         = search,
            SortBy         = sortBy,
            SortDirection  = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Educators.Read)]
    [ProducesResponseType(typeof(ApiResponse<EducatorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetEducatorQuery(id), ct);
        return OkResult(result);
    }

    // ── Availability &amp; Utilization Queries ──────────────────────────────────────

    [HttpGet("{id:guid}/availability")]
    [HasPermission(Permissions.Educators.Read)]
    [ProducesResponseType(typeof(ApiResponse<EducatorAvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailability(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetEducatorAvailabilityQuery(id), ct);
        return OkResult(result);
    }

    [HttpGet("utilization")]
    [HasPermission(Permissions.Educators.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EducatorUtilizationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUtilization(
        [FromQuery] Guid corporationId,
        [FromQuery] Guid? campusId = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetEducatorUtilizationQuery
        {
            CorporationId = corporationId,
            CampusId      = campusId,
            ActiveOnly    = activeOnly
        }, ct);
        return OkResult(result);
    }

    // ── Educator CRUD Commands ────────────────────────────────────────────────

    [HttpPost]
    [HasPermission(Permissions.Educators.Create)]
    [ProducesResponseType(typeof(ApiResponse<EducatorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateEducatorRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateEducatorCommand(
            req.CorporationId, req.FirstName, req.LastName,
            req.TitleId, req.Email, req.Phone,
            req.EmploymentType, req.HireDate, req.PrimaryCampusId), ct);
        return CreatedResult(result, $"/api/educators/{result.Id}");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Educators.Update)]
    [ProducesResponseType(typeof(ApiResponse<EducatorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEducatorRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateEducatorCommand(
            id, req.FirstName, req.LastName, req.TitleId,
            req.Email, req.Phone, req.EmploymentType,
            req.HireDate, req.PrimaryCampusId, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Educators.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteEducatorCommand(id), ct);
        return NoContentResult("Educator deleted.");
    }

    // ── Activate / Deactivate ─────────────────────────────────────────────────

    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.Educators.Update)]
    [ProducesResponseType(typeof(ApiResponse<EducatorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new ActivateEducatorCommand(id), ct);
        return OkResult(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Educators.Update)]
    [ProducesResponseType(typeof(ApiResponse<EducatorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeactivateEducatorCommand(id), ct);
        return OkResult(result);
    }

    // ── Specialty Assignment Workflow ─────────────────────────────────────────

    [HttpPost("{id:guid}/specialties")]
    [HasPermission(Permissions.Educators.ManageSpecialties)]
    [ProducesResponseType(typeof(ApiResponse<EducatorSpecialtyDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignSpecialty(
        Guid id, [FromBody] AssignSpecialtyRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AssignSpecialtyCommand(id, req.SpecialtyId), ct);
        return CreatedResult(result, $"/api/educators/{id}/specialties/{result.Id}");
    }

    [HttpDelete("{id:guid}/specialties/{assignmentId:guid}")]
    [HasPermission(Permissions.Educators.ManageSpecialties)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveSpecialty(Guid id, Guid assignmentId, CancellationToken ct)
    {
        await Sender.Send(new RemoveSpecialtyCommand(assignmentId), ct);
        return NoContentResult("Specialty removed.");
    }

    // ── Campus Assignment Workflow ────────────────────────────────────────────

    [HttpPost("{id:guid}/campuses")]
    [HasPermission(Permissions.Educators.ManageCampuses)]
    [ProducesResponseType(typeof(ApiResponse<EducatorCampusDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignCampus(
        Guid id, [FromBody] AssignCampusRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new AssignEducatorToCampusCommand(id, req.CampusId, req.IsPrimary, req.ActiveFrom), ct);
        return CreatedResult(result, $"/api/educators/{id}/campuses/{result.Id}");
    }

    [HttpPatch("{id:guid}/campuses/{assignmentId:guid}/end")]
    [HasPermission(Permissions.Educators.ManageCampuses)]
    [ProducesResponseType(typeof(ApiResponse<EducatorCampusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EndCampusAssignment(
        Guid id, Guid assignmentId, [FromBody] EndCampusAssignmentRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new EndEducatorCampusAssignmentCommand(assignmentId, req.EndDate), ct);
        return OkResult(result);
    }

    // ── Certification Workflow ────────────────────────────────────────────────

    [HttpPost("{id:guid}/certifications")]
    [HasPermission(Permissions.Educators.ManageCertifications)]
    [ProducesResponseType(typeof(ApiResponse<EducatorCertificationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddCertification(
        Guid id, [FromBody] AddCertificationRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AddCertificationCommand(
            id, req.Name, req.CertificationTypeId,
            req.Issuer, req.IssuedOn, req.ExpiresOn, req.FileId), ct);
        return CreatedResult(result, $"/api/educators/{id}/certifications/{result.Id}");
    }

    [HttpPut("{id:guid}/certifications/{certId:guid}")]
    [HasPermission(Permissions.Educators.ManageCertifications)]
    [ProducesResponseType(typeof(ApiResponse<EducatorCertificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCertification(
        Guid id, Guid certId, [FromBody] UpdateCertificationRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateCertificationCommand(
            certId, req.Name, req.CertificationTypeId,
            req.Issuer, req.IssuedOn, req.ExpiresOn, req.FileId, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}/certifications/{certId:guid}")]
    [HasPermission(Permissions.Educators.ManageCertifications)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCertification(Guid id, Guid certId, CancellationToken ct)
    {
        await Sender.Send(new DeleteCertificationCommand(certId), ct);
        return NoContentResult("Certification deleted.");
    }

    // ── Hierarchy (Educator Assignment Workflow) ──────────────────────────────

    [HttpPost("{id:guid}/hierarchy")]
    [HasPermission(Permissions.Educators.ManageHierarchy)]
    [ProducesResponseType(typeof(ApiResponse<EducatorHierarchyDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> LinkHierarchy(
        Guid id, [FromBody] LinkHierarchyRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new LinkHierarchyCommand(
            id, req.SupervisorId, req.RelationshipId, req.CampusId, req.ActiveFrom), ct);
        return CreatedResult(result, $"/api/educators/{id}/hierarchy/{result.Id}");
    }

    [HttpPatch("{id:guid}/hierarchy/{edgeId:guid}/end")]
    [HasPermission(Permissions.Educators.ManageHierarchy)]
    [ProducesResponseType(typeof(ApiResponse<EducatorHierarchyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EndHierarchy(
        Guid id, Guid edgeId, [FromBody] EndHierarchyRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new EndHierarchyCommand(edgeId, req.EndDate), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}/hierarchy/{edgeId:guid}")]
    [HasPermission(Permissions.Educators.ManageHierarchy)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnlinkHierarchy(Guid id, Guid edgeId, CancellationToken ct)
    {
        await Sender.Send(new UnlinkHierarchyCommand(edgeId), ct);
        return NoContentResult("Hierarchy edge removed.");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateEducatorRequest(
    Guid CorporationId,
    string FirstName,
    string LastName,
    Guid? TitleId,
    string? Email,
    string? Phone,
    string? EmploymentType,
    DateOnly? HireDate,
    Guid? PrimaryCampusId);

public record UpdateEducatorRequest(
    string FirstName,
    string LastName,
    Guid? TitleId,
    string? Email,
    string? Phone,
    string? EmploymentType,
    DateOnly? HireDate,
    Guid? PrimaryCampusId,
    int RowVersion);

public record AssignSpecialtyRequest(Guid SpecialtyId);

public record AssignCampusRequest(Guid CampusId, bool IsPrimary, DateOnly? ActiveFrom);

public record EndCampusAssignmentRequest(DateOnly? EndDate);

public record AddCertificationRequest(
    string Name,
    Guid? CertificationTypeId,
    string? Issuer,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    Guid? FileId);

public record UpdateCertificationRequest(
    string Name,
    Guid? CertificationTypeId,
    string? Issuer,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    Guid? FileId,
    int RowVersion);

public record LinkHierarchyRequest(
    Guid SupervisorId,
    Guid? RelationshipId,
    Guid? CampusId,
    DateOnly? ActiveFrom);

public record EndHierarchyRequest(DateOnly? EndDate);
