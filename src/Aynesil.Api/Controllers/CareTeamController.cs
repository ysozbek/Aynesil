using Aynesil.Api.Authorization;
using Aynesil.Application.Features.CareTeam.Commands;
using Aynesil.Application.Features.CareTeam.Dtos;
using Aynesil.Application.Features.CareTeam.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Care-team assignment management.
/// All endpoints require authentication and are tenant-scoped via RLS.
///
/// Authorization:
///   - care_team:read  → read assignments
///   - care_team:assign → create / update / end assignments
///
/// Security: ABAC Phase 3 RESTRICTIVE RLS policies are the actual security backstop.
/// These endpoints additionally enforce RBAC permission checks before dispatching.
///
/// GUC wiring: app.care_team_bypass is set by TenantConnectionInterceptor from JWT perm claims.
/// </summary>
[Route("api")]
public sealed class CareTeamController : BaseController
{
    // ── Student-scoped endpoints ──────────────────────────────────────────────

    /// <summary>List all care-team assignments for a student.</summary>
    [HttpGet("students/{studentId:guid}/care-team")]
    [HasPermission(Permissions.CareTeam.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CareTeamAssignmentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentCareTeam(
        Guid studentId,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(
            new GetStudentCareTeamQuery(studentId, activeOnly), ct);
        return OkResult(result);
    }

    /// <summary>Get a single care-team assignment by ID.</summary>
    [HttpGet("students/{studentId:guid}/care-team/{assignmentId:guid}")]
    [HasPermission(Permissions.CareTeam.Read)]
    [ProducesResponseType(typeof(ApiResponse<CareTeamAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssignment(
        Guid studentId,
        Guid assignmentId,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetCareTeamAssignmentQuery(assignmentId), ct);
        return OkResult(result);
    }

    /// <summary>Assign an educator to a student's care team.</summary>
    [HttpPost("students/{studentId:guid}/care-team")]
    [HasPermission(Permissions.CareTeam.Assign)]
    [ProducesResponseType(typeof(ApiResponse<CareTeamAssignmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Assign(
        Guid studentId,
        [FromBody] AssignRequest body,
        CancellationToken ct = default)
    {
        var command = new AssignCareTeamMemberCommand(
            CorporationId:      body.CorporationId,
            StudentId:          studentId,
            EducatorId:         body.EducatorId,
            RoleId:             body.RoleId,
            IsPrimary:          body.IsPrimary,
            ActiveFrom:         body.ActiveFrom,
            ActiveTo:           body.ActiveTo,
            CampusId:           body.CampusId,
            GrantTypeId:        body.GrantTypeId,
            SourceAssignmentId: body.SourceAssignmentId,
            Reason:             body.Reason);

        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/students/{studentId}/care-team/{result.Id}");
    }

    /// <summary>Update dates, role, or primary status of an assignment.</summary>
    [HttpPut("students/{studentId:guid}/care-team/{assignmentId:guid}")]
    [HasPermission(Permissions.CareTeam.Assign)]
    [ProducesResponseType(typeof(ApiResponse<CareTeamAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid studentId,
        Guid assignmentId,
        [FromBody] UpdateRequest body,
        CancellationToken ct = default)
    {
        var command = new UpdateCareTeamAssignmentCommand(
            Id:         assignmentId,
            RoleId:     body.RoleId,
            IsPrimary:  body.IsPrimary,
            ActiveFrom: body.ActiveFrom,
            ActiveTo:   body.ActiveTo,
            CampusId:   body.CampusId,
            Reason:     body.Reason,
            RowVersion: body.RowVersion);

        var result = await Sender.Send(command, ct);
        return OkResult(result);
    }

    /// <summary>
    /// Soft-end a care-team assignment (sets ActiveTo = today, Status = ended).
    /// History is preserved; no physical delete occurs.
    /// </summary>
    [HttpDelete("students/{studentId:guid}/care-team/{assignmentId:guid}")]
    [HasPermission(Permissions.CareTeam.Assign)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        Guid studentId,
        Guid assignmentId,
        [FromBody] RemoveRequest? body,
        CancellationToken ct = default)
    {
        await Sender.Send(
            new RemoveCareTeamAssignmentCommand(
                assignmentId, body?.Reason, body?.RowVersion ?? 0), ct);
        return NoContentResult("Assignment ended successfully.");
    }

    // ── Educator-scoped endpoint ──────────────────────────────────────────────

    /// <summary>List all active students assigned to the current authenticated educator.</summary>
    [HttpGet("care-team/my-students")]
    [HasPermission(Permissions.CareTeam.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CareTeamStudentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyStudents(CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetMyCareTeamStudentsQuery(), ct);
        return OkResult(result);
    }

    // ── Request body records ─────────────────────────────────────────────────

    public record AssignRequest(
        Guid     CorporationId,
        Guid     EducatorId,
        Guid     RoleId,
        bool     IsPrimary,
        DateOnly ActiveFrom,
        DateOnly? ActiveTo,
        Guid?    CampusId,
        Guid?    GrantTypeId,
        Guid?    SourceAssignmentId,
        string?  Reason);

    public record UpdateRequest(
        Guid?     RoleId,
        bool?     IsPrimary,
        DateOnly? ActiveFrom,
        DateOnly? ActiveTo,
        Guid?     CampusId,
        string?   Reason,
        int       RowVersion);

    public record RemoveRequest(
        string? Reason,
        int     RowVersion);
}
