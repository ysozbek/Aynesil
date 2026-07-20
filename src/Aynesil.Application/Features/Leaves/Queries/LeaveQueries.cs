using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leaves.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leaves.Queries;

// ── GetLeaveRequestsQuery ─────────────────────────────────────────────────────

/// <summary>
/// Paginated list of leave requests.
/// Supports filtering by corporation, educator, leave type, status, and date range.
/// </summary>
public class GetLeaveRequestsQuery : PagedQuery, IRequest<PaginatedResult<LeaveRequestListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? EducatorId { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public string? Status { get; set; }
    public string? Unit { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed class GetLeaveRequestsQueryHandler
    : IRequestHandler<GetLeaveRequestsQuery, PaginatedResult<LeaveRequestListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetLeaveRequestsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<LeaveRequestListItemDto>> Handle(
        GetLeaveRequestsQuery req, CancellationToken ct)
    {
        var q = _db.LeaveRequests.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(lr => lr.CorporationId == req.CorporationId.Value);

        if (req.EducatorId.HasValue)
            q = q.Where(lr => lr.EducatorId == req.EducatorId.Value);

        if (req.LeaveTypeId.HasValue)
            q = q.Where(lr => lr.LeaveTypeId == req.LeaveTypeId.Value);

        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(lr => lr.Status == req.Status);

        if (!string.IsNullOrWhiteSpace(req.Unit))
            q = q.Where(lr => lr.Unit == req.Unit);

        if (req.From.HasValue)
            q = q.Where(lr => lr.EndsAt >= req.From.Value);

        if (req.To.HasValue)
            q = q.Where(lr => lr.StartsAt <= req.To.Value);

        var query =
            from lr in q
            join ed in _db.Educators.AsNoTracking()
                on lr.EducatorId equals ed.Id
            join typ in _db.RefValues.AsNoTracking()
                on lr.LeaveTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new LeaveRequestListItemDto(
                lr.Id,
                lr.CorporationId,
                lr.EducatorId,
                ed.FirstName + " " + ed.LastName,
                lr.LeaveTypeId,
                typ != null ? typ.Code : null,
                lr.Unit,
                lr.StartsAt,
                lr.EndsAt,
                lr.Quantity,
                lr.Status,
                lr.CreatedAt,
                lr.UpdatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "startsat"  => req.IsDescending ? query.OrderByDescending(x => x.StartsAt)  : query.OrderBy(x => x.StartsAt),
            "endsat"    => req.IsDescending ? query.OrderByDescending(x => x.EndsAt)    : query.OrderBy(x => x.EndsAt),
            "status"    => req.IsDescending ? query.OrderByDescending(x => x.Status)    : query.OrderBy(x => x.Status),
            "educator"  => req.IsDescending ? query.OrderByDescending(x => x.EducatorFullName) : query.OrderBy(x => x.EducatorFullName),
            "createdat" => req.IsDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _           => query.OrderByDescending(x => x.StartsAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<LeaveRequestListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetLeaveRequestQuery ──────────────────────────────────────────────────────

/// <summary>Full leave request detail with approval history.</summary>
public record GetLeaveRequestQuery(Guid Id) : IRequest<LeaveRequestDto>;

public sealed class GetLeaveRequestQueryHandler : IRequestHandler<GetLeaveRequestQuery, LeaveRequestDto>
{
    private readonly IAppDbContext _db;

    public GetLeaveRequestQueryHandler(IAppDbContext db) => _db = db;

    public async Task<LeaveRequestDto> Handle(GetLeaveRequestQuery req, CancellationToken ct)
    {
        var leave = await _db.LeaveRequests.AsNoTracking()
            .Include(lr => lr.Approvals)
            .FirstOrDefaultAsync(lr => lr.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"LeaveRequest {req.Id} not found.");

        var educator = await _db.Educators.AsNoTracking()
            .Where(e => e.Id == leave.EducatorId)
            .Select(e => new { e.FirstName, e.LastName })
            .FirstOrDefaultAsync(ct);

        var typeCode = leave.LeaveTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == leave.LeaveTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return new LeaveRequestDto(
            leave.Id,
            leave.CorporationId,
            leave.EducatorId,
            educator != null ? $"{educator.FirstName} {educator.LastName}" : null,
            leave.LeaveTypeId,
            typeCode,
            leave.Unit,
            leave.StartsAt,
            leave.EndsAt,
            leave.Quantity,
            leave.Reason,
            leave.Status,
            leave.CreatedAt,
            leave.CreatedBy,
            leave.UpdatedAt,
            leave.RowVersion,
            leave.Approvals
                .OrderBy(a => a.StepNo)
                .ThenBy(a => a.DecidedAt)
                .Select(a => new LeaveApprovalDto(
                    a.Id, a.LeaveRequestId, a.StepNo,
                    a.ApproverId, a.Decision, a.Comment, a.DecidedAt))
                .ToList());
    }
}

// ── GetLeaveCalendarQuery ─────────────────────────────────────────────────────

/// <summary>
/// Returns leave requests as calendar items within a date range.
/// Supports corporation-level and educator-level calendar contexts.
/// Pending and approved leaves are included; rejected/cancelled are excluded.
/// </summary>
public class GetLeaveCalendarQuery : IRequest<IReadOnlyList<LeaveCalendarItemDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? EducatorId { get; set; }
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public bool IncludePending { get; set; } = true;
}

public sealed class GetLeaveCalendarQueryHandler
    : IRequestHandler<GetLeaveCalendarQuery, IReadOnlyList<LeaveCalendarItemDto>>
{
    private readonly IAppDbContext _db;

    public GetLeaveCalendarQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeaveCalendarItemDto>> Handle(
        GetLeaveCalendarQuery req, CancellationToken ct)
    {
        var q = _db.LeaveRequests.AsNoTracking()
            .Where(lr => lr.CorporationId == req.CorporationId
                      && (lr.Status == "approved" || (req.IncludePending && lr.Status == "pending")));

        // Overlap with requested window.
        q = q.Where(lr => lr.StartsAt < req.To && lr.EndsAt > req.From);

        if (req.EducatorId.HasValue)
            q = q.Where(lr => lr.EducatorId == req.EducatorId.Value);

        var results = await (
            from lr in q
            join ed in _db.Educators.AsNoTracking()
                on lr.EducatorId equals ed.Id
            join typ in _db.RefValues.AsNoTracking()
                on lr.LeaveTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            orderby lr.StartsAt
            select new LeaveCalendarItemDto(
                lr.Id,
                lr.EducatorId,
                ed.FirstName + " " + ed.LastName,
                lr.LeaveTypeId,
                typ != null ? typ.Code : null,
                lr.Unit,
                lr.StartsAt,
                lr.EndsAt,
                lr.Quantity,
                lr.Status)
        ).ToListAsync(ct);

        return results;
    }
}

// ── GetLeaveBalancesQuery ─────────────────────────────────────────────────────

/// <summary>Leave balance records for an educator or across a corporation for a given year.</summary>
public class GetLeaveBalancesQuery : IRequest<IReadOnlyList<LeaveBalanceDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? EducatorId { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public int? PeriodYear { get; set; }
}

public sealed class GetLeaveBalancesQueryHandler
    : IRequestHandler<GetLeaveBalancesQuery, IReadOnlyList<LeaveBalanceDto>>
{
    private readonly IAppDbContext _db;

    public GetLeaveBalancesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeaveBalanceDto>> Handle(
        GetLeaveBalancesQuery req, CancellationToken ct)
    {
        var q = _db.LeaveBalances.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(b => b.CorporationId == req.CorporationId.Value);

        if (req.EducatorId.HasValue)
            q = q.Where(b => b.EducatorId == req.EducatorId.Value);

        if (req.LeaveTypeId.HasValue)
            q = q.Where(b => b.LeaveTypeId == req.LeaveTypeId.Value);

        if (req.PeriodYear.HasValue)
            q = q.Where(b => b.PeriodYear == req.PeriodYear.Value);

        var results = await (
            from b in q
            join ed in _db.Educators.AsNoTracking()
                on b.EducatorId equals ed.Id
            join typ in _db.RefValues.AsNoTracking()
                on b.LeaveTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            orderby ed.LastName, ed.FirstName, b.PeriodYear
            select new LeaveBalanceDto(
                b.Id,
                b.CorporationId,
                b.EducatorId,
                ed.FirstName + " " + ed.LastName,
                b.LeaveTypeId,
                typ != null ? typ.Code : null,
                b.PeriodYear,
                b.Entitled,
                b.Used,
                b.Entitled - b.Used,
                b.Unit)
        ).ToListAsync(ct);

        return results;
    }
}

// ── GetLeaveSessionImpactQuery ────────────────────────────────────────────────

/// <summary>
/// Returns sessions scheduled for the educator during the leave period.
/// Allows approvers to identify sessions that need to be covered or rescheduled.
/// Only returns 'scheduled' sessions — already cancelled/completed ones are excluded.
/// </summary>
public record GetLeaveSessionImpactQuery(Guid LeaveRequestId) : IRequest<IReadOnlyList<LeaveSessionImpactDto>>;

public sealed class GetLeaveSessionImpactQueryHandler
    : IRequestHandler<GetLeaveSessionImpactQuery, IReadOnlyList<LeaveSessionImpactDto>>
{
    private readonly IAppDbContext _db;

    public GetLeaveSessionImpactQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeaveSessionImpactDto>> Handle(
        GetLeaveSessionImpactQuery req, CancellationToken ct)
    {
        var leave = await _db.LeaveRequests.AsNoTracking()
            .FirstOrDefaultAsync(lr => lr.Id == req.LeaveRequestId, ct)
            ?? throw new KeyNotFoundException($"LeaveRequest {req.LeaveRequestId} not found.");

        var impactedSessionIds = _db.SessionEducators.AsNoTracking()
            .Where(se => se.EducatorId == leave.EducatorId)
            .Select(se => se.SessionId);

        var sessions = await _db.Sessions.AsNoTracking()
            .Where(s =>
                impactedSessionIds.Contains(s.Id)
                && s.Status == "scheduled"
                && s.StartsAt < leave.EndsAt
                && s.EndsAt > leave.StartsAt)
            .OrderBy(s => s.StartsAt)
            .Select(s => new LeaveSessionImpactDto(
                s.Id,
                s.StartsAt,
                s.EndsAt,
                s.Title,
                s.Status))
            .ToListAsync(ct);

        return sessions;
    }
}

// ── GetLeaveUsageReportQuery ──────────────────────────────────────────────────

/// <summary>
/// Leave usage report: per-educator summary of entitled, used, and remaining leave
/// for a given year and optional leave type filter.
/// </summary>
public class GetLeaveUsageReportQuery : IRequest<IReadOnlyList<LeaveUsageReportItemDto>>
{
    public Guid CorporationId { get; set; }
    public int PeriodYear { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public Guid? EducatorId { get; set; }
}

public sealed class GetLeaveUsageReportQueryHandler
    : IRequestHandler<GetLeaveUsageReportQuery, IReadOnlyList<LeaveUsageReportItemDto>>
{
    private readonly IAppDbContext _db;

    public GetLeaveUsageReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeaveUsageReportItemDto>> Handle(
        GetLeaveUsageReportQuery req, CancellationToken ct)
    {
        var balanceQ = _db.LeaveBalances.AsNoTracking()
            .Where(b =>
                b.CorporationId == req.CorporationId
                && b.PeriodYear == req.PeriodYear);

        if (req.LeaveTypeId.HasValue)
            balanceQ = balanceQ.Where(b => b.LeaveTypeId == req.LeaveTypeId.Value);

        if (req.EducatorId.HasValue)
            balanceQ = balanceQ.Where(b => b.EducatorId == req.EducatorId.Value);

        // Approved request counts per educator+type for the period.
        var requestCountQ =
            from lr in _db.LeaveRequests.AsNoTracking()
            where lr.CorporationId == req.CorporationId
               && lr.Status == "approved"
               && lr.StartsAt.Year == req.PeriodYear
               && (!req.LeaveTypeId.HasValue || lr.LeaveTypeId == req.LeaveTypeId)
               && (!req.EducatorId.HasValue || lr.EducatorId == req.EducatorId)
            group lr by new { lr.EducatorId, lr.LeaveTypeId } into g
            select new
            {
                g.Key.EducatorId,
                g.Key.LeaveTypeId,
                Count = g.Count()
            };

        var result = await (
            from b in balanceQ
            join ed in _db.Educators.AsNoTracking() on b.EducatorId equals ed.Id
            join typ in _db.RefValues.AsNoTracking()
                on b.LeaveTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            join rc in requestCountQ
                on new { b.EducatorId, b.LeaveTypeId } equals new { rc.EducatorId, rc.LeaveTypeId } into rcGrp
            from rc in rcGrp.DefaultIfEmpty()
            orderby ed.LastName, ed.FirstName, b.PeriodYear
            select new LeaveUsageReportItemDto(
                b.EducatorId,
                ed.FirstName + " " + ed.LastName,
                b.LeaveTypeId,
                typ != null ? typ.Code : null,
                b.PeriodYear,
                b.Entitled,
                b.Used,
                b.Entitled - b.Used,
                b.Unit,
                rc != null ? rc.Count : 0)
        ).ToListAsync(ct);

        return result;
    }
}

// ── GetLeaveTrendsQuery ───────────────────────────────────────────────────────

/// <summary>
/// Monthly leave request trends for a given year range.
/// Useful for HR dashboards to identify patterns (peak leave months, approval rates, etc.).
/// </summary>
public class GetLeaveTrendsQuery : IRequest<IReadOnlyList<LeaveTrendItemDto>>
{
    public Guid CorporationId { get; set; }
    public int FromYear { get; set; }
    public int ToYear { get; set; }
    public Guid? LeaveTypeId { get; set; }
}

public sealed class GetLeaveTrendsQueryHandler
    : IRequestHandler<GetLeaveTrendsQuery, IReadOnlyList<LeaveTrendItemDto>>
{
    private readonly IAppDbContext _db;

    public GetLeaveTrendsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeaveTrendItemDto>> Handle(
        GetLeaveTrendsQuery req, CancellationToken ct)
    {
        var q = _db.LeaveRequests.AsNoTracking()
            .Where(lr =>
                lr.CorporationId == req.CorporationId
                && lr.CreatedAt.Year >= req.FromYear
                && lr.CreatedAt.Year <= req.ToYear);

        if (req.LeaveTypeId.HasValue)
            q = q.Where(lr => lr.LeaveTypeId == req.LeaveTypeId.Value);

        var result = await (
            from lr in q
            group lr by new { lr.CreatedAt.Year, lr.CreatedAt.Month } into g
            orderby g.Key.Year, g.Key.Month
            select new LeaveTrendItemDto(
                g.Key.Year,
                g.Key.Month,
                g.Count(),
                g.Count(lr => lr.Status == "approved"),
                g.Count(lr => lr.Status == "rejected"),
                g.Count(lr => lr.Status == "cancelled"),
                g.Where(lr => lr.Status == "approved" && lr.Quantity != null)
                 .Sum(lr => lr.Quantity ?? 0m))
        ).ToListAsync(ct);

        return result;
    }
}
