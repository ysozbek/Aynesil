using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Queries;

// ── GetCampReportsQuery ───────────────────────────────────────────────────────

public record GetCampReportsQuery(Guid CampEnrollmentId)
    : IRequest<IReadOnlyList<CampReportDto>>;

public sealed class GetCampReportsQueryHandler
    : IRequestHandler<GetCampReportsQuery, IReadOnlyList<CampReportDto>>
{
    private readonly IAppDbContext _db;

    public GetCampReportsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CampReportDto>> Handle(
        GetCampReportsQuery req, CancellationToken ct)
    {
        return await _db.CampReports.AsNoTracking()
            .Where(r => r.CampEnrollmentId == req.CampEnrollmentId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new CampReportDto(
                r.Id, r.CampEnrollmentId,
                r.Summary, r.FileId,
                r.AuthoredBy, r.CreatedAt))
            .ToListAsync(ct);
    }
}

// ── GetCampEnrollmentSummaryQuery ─────────────────────────────────────────────

/// <summary>
/// Enrollment summary per period for a camp.
/// Used for camp enrollment report and capacity management view.
/// </summary>
public record GetCampEnrollmentSummaryQuery(Guid CampId)
    : IRequest<IReadOnlyList<CampEnrollmentSummaryDto>>;

public sealed class GetCampEnrollmentSummaryQueryHandler
    : IRequestHandler<GetCampEnrollmentSummaryQuery, IReadOnlyList<CampEnrollmentSummaryDto>>
{
    private readonly IAppDbContext _db;

    public GetCampEnrollmentSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CampEnrollmentSummaryDto>> Handle(
        GetCampEnrollmentSummaryQuery req, CancellationToken ct)
    {
        return await (
            from p in _db.CampPeriods.AsNoTracking()
            where p.CampId == req.CampId
            select new CampEnrollmentSummaryDto(
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.Capacity,
                p.Enrollments.Count(e => e.Status == "enrolled"),
                p.Enrollments.Count(e => e.Status == "waitlist"),
                p.Enrollments.Count(e => e.Status == "withdrawn"),
                p.Enrollments.Count(e => e.Status == "completed"))
        ).OrderBy(x => x.StartDate).ToListAsync(ct);
    }
}

// ── GetCampPerformanceQuery ───────────────────────────────────────────────────

/// <summary>
/// Camp-level performance report: enrollment rates, completion rates, attendance rates.
/// Filterable by corporation and campus. Used for management dashboards.
/// </summary>
public class GetCampPerformanceQuery : IRequest<IReadOnlyList<CampPerformanceDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? CampTypeId { get; set; }
}

public sealed class GetCampPerformanceQueryHandler
    : IRequestHandler<GetCampPerformanceQuery, IReadOnlyList<CampPerformanceDto>>
{
    private readonly IAppDbContext _db;

    public GetCampPerformanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CampPerformanceDto>> Handle(
        GetCampPerformanceQuery req, CancellationToken ct)
    {
        var q = _db.Camps.AsNoTracking()
            .Where(c => c.CorporationId == req.CorporationId && c.DeletedAt == null);

        if (req.CampusId.HasValue)
            q = q.Where(c => c.CampusId == req.CampusId.Value);

        if (req.CampTypeId.HasValue)
            q = q.Where(c => c.CampTypeId == req.CampTypeId.Value);

        // Nested Sum+Count translate to correlated SQL subqueries — no N+1.
        var rawData = await q.Select(c => new
        {
            c.Id,
            c.Code,
            c.Name,
            PeriodCount    = c.Periods.Count,
            TotalEnrolled  = c.Periods.Sum(p => p.Enrollments.Count(e => e.Status != "withdrawn")),
            TotalCompleted = c.Periods.Sum(p => p.Enrollments.Count(e => e.Status == "completed")),
            TotalWithdrawn = c.Periods.Sum(p => p.Enrollments.Count(e => e.Status == "withdrawn")),
            TotalAttendanceDays = c.Periods.Sum(p =>
                p.Enrollments.Sum(e => e.Attendances.Count())),
            TotalPresentDays = c.Periods.Sum(p =>
                p.Enrollments.Sum(e => e.Attendances.Count(a => a.Status == "present")))
        }).ToListAsync(ct);

        return rawData.Select(d =>
        {
            var completionRate = d.TotalEnrolled > 0
                ? Math.Round((double)d.TotalCompleted / d.TotalEnrolled * 100, 1)
                : 0;
            var attendanceRate = d.TotalAttendanceDays > 0
                ? Math.Round((double)d.TotalPresentDays / d.TotalAttendanceDays * 100, 1)
                : 0;

            return new CampPerformanceDto(
                d.Id, d.Code, d.Name,
                d.PeriodCount,
                d.TotalEnrolled,
                d.TotalCompleted,
                d.TotalWithdrawn,
                completionRate,
                attendanceRate);
        }).ToList();
    }
}
