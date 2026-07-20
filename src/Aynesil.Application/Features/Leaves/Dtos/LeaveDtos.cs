namespace Aynesil.Application.Features.Leaves.Dtos;

// ── Leave Request DTOs ────────────────────────────────────────────────────────

public record LeaveRequestListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid EducatorId,
    string? EducatorFullName,
    Guid? LeaveTypeId,
    string? LeaveTypeCode,
    string Unit,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal? Quantity,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record LeaveRequestDto(
    Guid Id,
    Guid CorporationId,
    Guid EducatorId,
    string? EducatorFullName,
    Guid? LeaveTypeId,
    string? LeaveTypeCode,
    string Unit,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal? Quantity,
    string? Reason,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<LeaveApprovalDto> Approvals);

// ── Leave Approval DTOs ───────────────────────────────────────────────────────

public record LeaveApprovalDto(
    Guid Id,
    Guid LeaveRequestId,
    int StepNo,
    Guid? ApproverId,
    string Decision,
    string? Comment,
    DateTimeOffset? DecidedAt);

// ── Leave Balance DTOs ────────────────────────────────────────────────────────

public record LeaveBalanceDto(
    Guid Id,
    Guid CorporationId,
    Guid EducatorId,
    string? EducatorFullName,
    Guid? LeaveTypeId,
    string? LeaveTypeCode,
    int PeriodYear,
    decimal Entitled,
    decimal Used,
    decimal Remaining,
    string Unit);

// ── Calendar DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Lightweight leave entry for calendar views.
/// Supports educator-level and corporation-level calendar contexts.
/// </summary>
public record LeaveCalendarItemDto(
    Guid Id,
    Guid EducatorId,
    string? EducatorFullName,
    Guid? LeaveTypeId,
    string? LeaveTypeCode,
    string Unit,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal? Quantity,
    string Status);

// ── Session Impact DTOs ───────────────────────────────────────────────────────

/// <summary>
/// A session that overlaps with an approved/pending leave period.
/// Used by approvers to assess substitution needs before finalising approval.
/// </summary>
public record LeaveSessionImpactDto(
    Guid SessionId,
    DateTimeOffset SessionStartsAt,
    DateTimeOffset SessionEndsAt,
    string? SessionTitle,
    string SessionStatus);

// ── Report DTOs ───────────────────────────────────────────────────────────────

/// <summary>Per-educator leave usage summary for a given period year.</summary>
public record LeaveUsageReportItemDto(
    Guid EducatorId,
    string EducatorFullName,
    Guid? LeaveTypeId,
    string? LeaveTypeCode,
    int PeriodYear,
    decimal Entitled,
    decimal Used,
    decimal Remaining,
    string Unit,
    int RequestCount);

/// <summary>Monthly leave request count for trend analysis.</summary>
public record LeaveTrendItemDto(
    int Year,
    int Month,
    int RequestCount,
    int ApprovedCount,
    int RejectedCount,
    int CancelledCount,
    decimal TotalDaysApproved);
