using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Leaves.Commands;
using Aynesil.Application.Features.Leaves.Dtos;
using Aynesil.Application.Features.Leaves.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Leave management: requests, approvals, balances, session impact, calendar, reports.
/// Route: /api/leave
/// </summary>
[Route("api/leave")]
public sealed class LeaveController : BaseController
{
    // ── Leave Requests ────────────────────────────────────────────────────────────

    /// <summary>List leave requests (paginated). Filterable by status, type, educator, date range.</summary>
    [HttpGet("requests")]
    [HasPermission(Permissions.LeaveRequests.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<LeaveRequestListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] GetLeaveRequestsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Get full leave request detail including approval history.</summary>
    [HttpGet("requests/{id:guid}")]
    [HasPermission(Permissions.LeaveRequests.Read)]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveRequest(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetLeaveRequestQuery(id), ct));

    /// <summary>Submit a new leave request.</summary>
    [HttpPost("requests")]
    [HasPermission(Permissions.LeaveRequests.Submit)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitLeaveRequest(
        [FromBody] SubmitLeaveRequestCommand command, CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedResult(id, $"/api/leave/requests/{id}");
    }

    /// <summary>Update a pending leave request (only allowed while status is 'pending').</summary>
    [HttpPut("requests/{id:guid}")]
    [HasPermission(Permissions.LeaveRequests.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateLeaveRequest(
        Guid id, [FromBody] UpdateLeaveRequestCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    // ── Workflow Transitions ──────────────────────────────────────────────────────

    /// <summary>
    /// Approve a pending leave request.
    /// Validates balance sufficiency, deducts from leave_balance, records approval step.
    /// </summary>
    [HttpPost("requests/{id:guid}/approve")]
    [HasPermission(Permissions.LeaveRequests.Approve)]
    [ProducesResponseType(typeof(ApiResponse<LeaveApprovalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveLeaveRequest(
        Guid id, [FromBody] ApproveLeaveRequestCommand command, CancellationToken ct)
    {
        var approval = await Sender.Send(command with { Id = id }, ct);
        return OkResult(approval);
    }

    /// <summary>Reject a pending leave request and record the rejection decision.</summary>
    [HttpPost("requests/{id:guid}/reject")]
    [HasPermission(Permissions.LeaveRequests.Reject)]
    [ProducesResponseType(typeof(ApiResponse<LeaveApprovalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectLeaveRequest(
        Guid id, [FromBody] RejectLeaveRequestCommand command, CancellationToken ct)
    {
        var approval = await Sender.Send(command with { Id = id }, ct);
        return OkResult(approval);
    }

    /// <summary>
    /// Cancel a pending or approved leave request.
    /// Restores balance automatically if the leave was already approved.
    /// </summary>
    [HttpPost("requests/{id:guid}/cancel")]
    [HasPermission(Permissions.LeaveRequests.Cancel)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelLeaveRequest(Guid id, CancellationToken ct)
    {
        await Sender.Send(new CancelLeaveRequestCommand(id), ct);
        return NoContentResult();
    }

    // ── Session Impact ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all scheduled sessions overlapping with the leave period for the educator.
    /// Use before approving to identify sessions that require a substitute or rescheduling.
    /// </summary>
    [HttpGet("requests/{id:guid}/session-impact")]
    [HasPermission(Permissions.LeaveRequests.Approve)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveSessionImpactDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionImpact(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetLeaveSessionImpactQuery(id), ct));

    // ── Calendar ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calendar view: approved (and optionally pending) leave entries within a date range.
    /// Supports corporation-level and educator-level contexts.
    /// </summary>
    [HttpGet("calendar")]
    [HasPermission(Permissions.LeaveRequests.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveCalendarItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] GetLeaveCalendarQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    // ── Leave Balances ────────────────────────────────────────────────────────────

    /// <summary>Query leave balance records. Filterable by educator, type, and year.</summary>
    [HttpGet("balances")]
    [HasPermission(Permissions.LeaveBalances.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveBalanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaveBalances(
        [FromQuery] GetLeaveBalancesQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Initialize a leave balance record for an educator, leave type, and period year.</summary>
    [HttpPost("balances")]
    [HasPermission(Permissions.LeaveBalances.Manage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitializeLeaveBalance(
        [FromBody] InitializeLeaveBalanceCommand command, CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedResult(id, $"/api/leave/balances/{id}");
    }

    /// <summary>Adjust the entitled (total) leave days/hours for a balance record.</summary>
    [HttpPatch("balances/{id:guid}/entitlement")]
    [HasPermission(Permissions.LeaveBalances.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdjustLeaveEntitlement(
        Guid id, [FromBody] AdjustLeaveEntitlementCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>
    /// Carry forward unused leave from one period year to the next.
    /// Supports an optional MaxCarryForward cap.
    /// </summary>
    [HttpPost("balances/carry-forward")]
    [HasPermission(Permissions.LeaveBalances.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CarryForwardBalance(
        [FromBody] CarryForwardLeaveBalanceCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return NoContentResult();
    }

    // ── Reports ───────────────────────────────────────────────────────────────────

    /// <summary>Leave usage report: per-educator entitled / used / remaining summary for a period year.</summary>
    [HttpGet("reports/usage")]
    [HasPermission(Permissions.LeaveReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveUsageReportItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageReport(
        [FromQuery] GetLeaveUsageReportQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Leave trend report: monthly request/approval counts over a year range.</summary>
    [HttpGet("reports/trends")]
    [HasPermission(Permissions.LeaveReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveTrendItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrendsReport(
        [FromQuery] GetLeaveTrendsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));
}
