using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Notifications.Dtos;
using Aynesil.Application.Features.Portal.Dtos;
using Aynesil.Application.Features.Portal.Queries;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Application.Features.Students.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Parent Portal — read-only views scoped to the authenticated guardian's linked students.
/// Guardian identity is derived from the JWT (guardian's user_id).
/// No write operations — portal is read-only by design.
/// Route: /api/portal
/// </summary>
[Route("api/portal")]
public sealed class PortalController : BaseController
{
    // ── My Children ───────────────────────────────────────────────────────────────

    /// <summary>Returns the list of students the authenticated guardian may access via the portal.</summary>
    [HttpGet("my-students")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PortalStudentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyStudents(CancellationToken ct)
        => OkResult(await Sender.Send(new GetMyStudentsQuery(GetCurrentUserId()), ct));

    /// <summary>
    /// Returns a student summary for the guardian.
    /// Returns 403 if the guardian does not have active portal access to the student.
    /// </summary>
    [HttpGet("students/{studentId:guid}")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<PortalStudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentSummary(Guid studentId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPortalStudentSummaryQuery(studentId, GetCurrentUserId()), ct));

    /// <summary>
    /// Aggregate dashboard: upcoming sessions, unread notifications,
    /// package balance, active goals — for a single student.
    /// </summary>
    [HttpGet("students/{studentId:guid}/dashboard")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<PortalDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard(Guid studentId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPortalDashboardQuery(studentId, GetCurrentUserId()), ct));

    // ── Per-Student Read-Only Views ───────────────────────────────────────────────

    /// <summary>Development reports visible to the guardian.</summary>
    [HttpGet("students/{studentId:guid}/development-reports")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DevelopmentReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevelopmentReports(Guid studentId, CancellationToken ct)
    {
        await VerifyPortalAccess(studentId, ct);
        return OkResult(await Sender.Send(new GetDevelopmentReportsQuery(studentId), ct));
    }

    /// <summary>Session history for a student (guarded by CanViewSessions flag).</summary>
    [HttpGet("students/{studentId:guid}/sessions")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PortalSessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(
        Guid studentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetPortalSessionHistoryQuery(studentId, GetCurrentUserId(), page, pageSize), ct));

    /// <summary>Attendance history for a student (guarded by CanViewAttendance flag).</summary>
    [HttpGet("students/{studentId:guid}/attendance")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PortalAttendanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendance(
        Guid studentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetPortalAttendanceQuery(studentId, GetCurrentUserId(), page, pageSize), ct));

    /// <summary>Package balances for a student (guarded by CanViewFinance flag).</summary>
    [HttpGet("students/{studentId:guid}/packages")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PortalPackageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackages(Guid studentId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPortalPackagesQuery(studentId, GetCurrentUserId()), ct));

    /// <summary>Documents attached to a student (guarded by CanViewReports flag).</summary>
    [HttpGet("students/{studentId:guid}/documents")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PortalDocumentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments(Guid studentId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPortalDocumentsQuery(studentId, GetCurrentUserId()), ct));

    /// <summary>Approved education plans (BEP/IEP) visible to the guardian (guarded by CanViewPlan flag).</summary>
    [HttpGet("students/{studentId:guid}/bep")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PortalEducationPlanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBep(Guid studentId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPortalEducationPlanQuery(studentId, GetCurrentUserId()), ct));

    /// <summary>Active goal progress for a student (guarded by CanViewPlan flag).</summary>
    [HttpGet("students/{studentId:guid}/goal-progress")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PortalGoalProgressDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGoalProgress(Guid studentId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPortalGoalProgressQuery(studentId, GetCurrentUserId()), ct));

    /// <summary>Meeting history where the guardian is a participant.</summary>
    [HttpGet("students/{studentId:guid}/meetings")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PortalMeetingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMeetings(
        Guid studentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetPortalMeetingHistoryQuery(studentId, GetCurrentUserId(), page, pageSize), ct));

    // ── Notifications ─────────────────────────────────────────────────────────────

    /// <summary>In-app notification inbox for the authenticated guardian.</summary>
    [HttpGet("notifications")]
    [HasPermission(Permissions.Portal.Access)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetPortalNotificationsQuery(GetCurrentUserId(), page, pageSize), ct));

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = HttpContext.User.FindFirst("sub")?.Value
                 ?? HttpContext.User.FindFirst("userId")?.Value;
        if (!Guid.TryParse(claim, out var id))
            throw new UnauthorizedAccessException("Authenticated user identity not found.");
        return id;
    }

    private async Task VerifyPortalAccess(Guid studentId, CancellationToken ct)
        => await Sender.Send(new GetPortalStudentSummaryQuery(studentId, GetCurrentUserId()), ct);
}
