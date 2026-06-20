using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Application.Features.Students.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Parent Portal — read-only views scoped to the authenticated guardian's linked students.
/// All endpoints derive the guardian identity from the JWT (guardian's user_id).
/// No write operations — the portal is read-only by design.
/// Route: /api/portal
/// </summary>
[Route("api/portal")]
public sealed class PortalController : BaseController
{
    // ── My Children ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the list of students the authenticated guardian may access via the portal.
    /// Each entry includes the per-student visibility switches.
    /// </summary>
    [HttpGet("my-students")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PortalStudentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyStudents(CancellationToken ct)
    {
        var guardianUserId = GetCurrentUserId();
        var result = await Sender.Send(new GetMyStudentsQuery(guardianUserId), ct);
        return OkResult(result);
    }

    /// <summary>
    /// Returns a summary for a specific student the guardian has active portal access to.
    /// Returns 403 if the guardian does not have access to the requested student.
    /// </summary>
    [HttpGet("students/{studentId:guid}")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<PortalStudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentSummary(Guid studentId, CancellationToken ct)
    {
        var guardianUserId = GetCurrentUserId();
        var result = await Sender.Send(new GetPortalStudentSummaryQuery(studentId, guardianUserId), ct);
        return OkResult(result);
    }

    // ── Per-Student Read-Only Views ───────────────────────────────────────────

    /// <summary>Development reports visible to the guardian for a specific student.</summary>
    [HttpGet("students/{studentId:guid}/development-reports")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DevelopmentReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevelopmentReports(Guid studentId, CancellationToken ct)
    {
        await VerifyPortalAccess(studentId, ct);
        var result = await Sender.Send(new GetDevelopmentReportsQuery(studentId), ct);
        return OkResult(result);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.FindFirst("sub")?.Value
            ?? HttpContext.User.FindFirst("userId")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Authenticated user identity not found.");

        return userId;
    }

    private async Task VerifyPortalAccess(Guid studentId, CancellationToken ct)
    {
        var guardianUserId = GetCurrentUserId();
        await Sender.Send(new GetPortalStudentSummaryQuery(studentId, guardianUserId), ct);
    }
}
