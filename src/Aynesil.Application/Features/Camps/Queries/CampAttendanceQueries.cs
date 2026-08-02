using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Queries;

// ── GetCampAttendanceQuery ────────────────────────────────────────────────────

public class GetCampAttendanceQuery : PagedQuery, IRequest<PaginatedResult<CampAttendanceDto>>
{
    public Guid? CampEnrollmentId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? Status { get; set; }
}

public sealed class GetCampAttendanceQueryHandler
    : IRequestHandler<GetCampAttendanceQuery, PaginatedResult<CampAttendanceDto>>
{
    private readonly IAppDbContext _db;

    public GetCampAttendanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<CampAttendanceDto>> Handle(
        GetCampAttendanceQuery req, CancellationToken ct)
    {
        var q = _db.CampAttendances.AsNoTracking().AsQueryable();

        if (req.CampEnrollmentId.HasValue)
            q = q.Where(a => a.CampEnrollmentId == req.CampEnrollmentId.Value);

        if (req.DateFrom.HasValue)
            q = q.Where(a => a.AttendanceDate >= req.DateFrom.Value);

        if (req.DateTo.HasValue)
            q = q.Where(a => a.AttendanceDate <= req.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(a => a.Status == req.Status);

        var query = q.Select(a => new CampAttendanceDto(
            a.Id,
            a.CampEnrollmentId,
            a.AttendanceDate,
            a.Status,
            a.ReasonId,
            a.RecordedBy));

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "status" => req.IsDescending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _        => req.IsDescending ? query.OrderByDescending(x => x.AttendanceDate) : query.OrderBy(x => x.AttendanceDate)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<CampAttendanceDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetCampAttendanceSummaryQuery ─────────────────────────────────────────────

/// <summary>
/// Per-student attendance breakdown (present/absent/late/excused) for an entire camp period.
/// Used for camp attendance report.
/// </summary>
public record GetCampAttendanceSummaryQuery(Guid CampPeriodId)
    : IRequest<IReadOnlyList<CampAttendanceSummaryDto>>;

public sealed class GetCampAttendanceSummaryQueryHandler
    : IRequestHandler<GetCampAttendanceSummaryQuery, IReadOnlyList<CampAttendanceSummaryDto>>
{
    private readonly IAppDbContext _db;

    public GetCampAttendanceSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CampAttendanceSummaryDto>> Handle(
        GetCampAttendanceSummaryQuery req, CancellationToken ct)
    {
        // e.Attendances.Count(...) translates to SQL scalar subqueries — no N+1.
        var data = await (
            from e in _db.CampEnrollments.AsNoTracking()
            where e.CampPeriodId == req.CampPeriodId
            select new
            {
                EnrollmentId = e.Id,
                e.StudentId,
                Total   = e.Attendances.Count(),
                Present = e.Attendances.Count(a => a.Status == "present"),
                Absent  = e.Attendances.Count(a => a.Status == "absent"),
                Late    = e.Attendances.Count(a => a.Status == "late"),
                Excused = e.Attendances.Count(a => a.Status == "excused")
            }
        ).ToListAsync(ct);

        return data.Select(d =>
        {
            var rate = d.Total > 0
                ? Math.Round((double)d.Present / d.Total * 100, 1)
                : 0;
            return new CampAttendanceSummaryDto(
                d.EnrollmentId, d.StudentId,
                d.Total, d.Present, d.Absent, d.Late, d.Excused, rate);
        }).ToList();
    }
}
