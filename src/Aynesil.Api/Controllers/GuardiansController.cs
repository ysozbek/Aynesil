using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Students.Commands;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Application.Features.Students.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Guardian (parent/caregiver) management and portal access provisioning.
/// Route: /api/guardians
/// </summary>
[Route("api/guardians")]
public sealed class GuardiansController : BaseController
{
    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission(Permissions.Guardians.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GuardianListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] bool? hasPortalAccount = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetGuardiansQuery
        {
            CorporationId    = corporationId,
            HasPortalAccount = hasPortalAccount,
            Page             = page,
            PageSize         = pageSize,
            Search           = search,
            SortBy           = sortBy,
            SortDirection    = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Guardians.Read)]
    [ProducesResponseType(typeof(ApiResponse<GuardianDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetGuardianQuery(id), ct);
        return OkResult(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [HttpPost]
    [HasPermission(Permissions.Guardians.Create)]
    [ProducesResponseType(typeof(ApiResponse<GuardianDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateGuardianRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateGuardianCommand(
            req.CorporationId, req.FirstName, req.LastName,
            req.NationalId, req.Email, req.Phone, req.Occupation, req.AddressLine), ct);
        return CreatedResult(result, $"/api/guardians/{result.Id}");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Guardians.Update)]
    [ProducesResponseType(typeof(ApiResponse<GuardianDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGuardianRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateGuardianCommand(
            id, req.FirstName, req.LastName, req.NationalId,
            req.Email, req.Phone, req.Occupation, req.AddressLine, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Guardians.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteGuardianCommand(id), ct);
        return NoContentResult("Guardian deleted.");
    }

    // ── Portal Access ─────────────────────────────────────────────────────────

    /// <summary>
    /// Grants or re-enables parent portal access for the guardian/student pair.
    /// Creates the GuardianPortalAccess record and enables the portal_access flag
    /// on the StudentGuardian link.
    /// </summary>
    [HttpPost("{id:guid}/students/{studentId:guid}/portal-access")]
    [HasPermission(Permissions.Guardians.ManagePortal)]
    [ProducesResponseType(typeof(ApiResponse<GuardianPortalAccessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GrantPortalAccess(
        Guid id, Guid studentId,
        [FromBody] GrantPortalAccessRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new GrantPortalAccessCommand(
            id, studentId,
            req.CanViewSessions, req.CanViewAttendance, req.CanViewReports,
            req.CanViewPlan, req.CanViewFinance, req.CanViewCamera), ct);
        return OkResult(result);
    }

    /// <summary>Revokes parent portal access for the guardian/student pair.</summary>
    [HttpDelete("{id:guid}/students/{studentId:guid}/portal-access")]
    [HasPermission(Permissions.Guardians.ManagePortal)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokePortalAccess(
        Guid id, Guid studentId, CancellationToken ct)
    {
        await Sender.Send(new RevokePortalAccessCommand(id, studentId), ct);
        return NoContentResult("Portal access revoked.");
    }

    /// <summary>Updates which sections the guardian can see in the parent portal for this student.</summary>
    [HttpPut("{id:guid}/students/{studentId:guid}/portal-access")]
    [HasPermission(Permissions.Guardians.ManagePortal)]
    [ProducesResponseType(typeof(ApiResponse<GuardianPortalAccessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePortalPermissions(
        Guid id, Guid studentId,
        [FromBody] UpdatePortalPermissionsRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdatePortalPermissionsCommand(
            id, studentId,
            req.CanViewSessions, req.CanViewAttendance, req.CanViewReports,
            req.CanViewPlan, req.CanViewFinance, req.CanViewCamera), ct);
        return OkResult(result);
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateGuardianRequest(
    Guid CorporationId,
    string FirstName,
    string LastName,
    string? NationalId,
    string? Email,
    string? Phone,
    string? Occupation,
    string? AddressLine);

public record UpdateGuardianRequest(
    string FirstName,
    string LastName,
    string? NationalId,
    string? Email,
    string? Phone,
    string? Occupation,
    string? AddressLine,
    int RowVersion);

public record GrantPortalAccessRequest(
    bool CanViewSessions,
    bool CanViewAttendance,
    bool CanViewReports,
    bool CanViewPlan,
    bool CanViewFinance,
    bool CanViewCamera);

public record UpdatePortalPermissionsRequest(
    bool CanViewSessions,
    bool CanViewAttendance,
    bool CanViewReports,
    bool CanViewPlan,
    bool CanViewFinance,
    bool CanViewCamera);
