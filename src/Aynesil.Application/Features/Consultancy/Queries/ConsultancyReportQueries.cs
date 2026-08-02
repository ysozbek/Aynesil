using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Queries;

// ── GetConsultancyReportsQuery ────────────────────────────────────────────────

public class GetConsultancyReportsQuery
    : PagedQuery, IRequest<PaginatedResult<ConsultancyReportListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? ConsultancyPlanId { get; set; }
    public Guid? SchoolVisitId { get; set; }
}

public sealed class GetConsultancyReportsQueryHandler
    : IRequestHandler<GetConsultancyReportsQuery, PaginatedResult<ConsultancyReportListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetConsultancyReportsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ConsultancyReportListItemDto>> Handle(
        GetConsultancyReportsQuery req, CancellationToken ct)
    {
        var q = _db.ConsultancyReports.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(r => r.CorporationId == req.CorporationId.Value);
        if (req.ConsultancyPlanId.HasValue)
            q = q.Where(r => r.ConsultancyPlanId == req.ConsultancyPlanId.Value);
        if (req.SchoolVisitId.HasValue)
            q = q.Where(r => r.SchoolVisitId == req.SchoolVisitId.Value);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(r => r.Title.ToLower().Contains(term));
        }

        var query =
            from r in q
            join p in _db.ConsultancyPlans.AsNoTracking()
                on r.ConsultancyPlanId equals p.Id into planGrp
            from p in planGrp.DefaultIfEmpty()
            join v in _db.SchoolVisits.AsNoTracking()
                on r.SchoolVisitId equals v.Id into visitGrp
            from v in visitGrp.DefaultIfEmpty()
            select new ConsultancyReportListItemDto(
                r.Id,
                r.CorporationId,
                r.ConsultancyPlanId,
                p != null ? p.Name : null,
                r.SchoolVisitId,
                v != null ? v.VisitDate : (DateOnly?)null,
                r.Title,
                r.FileId != null,
                r.AuthoredBy,
                r.CreatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "title"     => req.IsDescending ? query.OrderByDescending(x => x.Title)     : query.OrderBy(x => x.Title),
            "createdat" => req.IsDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _           => query.OrderByDescending(x => x.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<ConsultancyReportListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetConsultancyReportQuery ─────────────────────────────────────────────────

public record GetConsultancyReportQuery(Guid Id) : IRequest<ConsultancyReportDto>;

public sealed class GetConsultancyReportQueryHandler
    : IRequestHandler<GetConsultancyReportQuery, ConsultancyReportDto>
{
    private readonly IAppDbContext _db;

    public GetConsultancyReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ConsultancyReportDto> Handle(
        GetConsultancyReportQuery req, CancellationToken ct)
    {
        return await (
            from r in _db.ConsultancyReports.AsNoTracking()
            where r.Id == req.Id
            join p in _db.ConsultancyPlans.AsNoTracking()
                on r.ConsultancyPlanId equals p.Id into planGrp
            from p in planGrp.DefaultIfEmpty()
            join v in _db.SchoolVisits.AsNoTracking()
                on r.SchoolVisitId equals v.Id into visitGrp
            from v in visitGrp.DefaultIfEmpty()
            select new ConsultancyReportDto(
                r.Id, r.CorporationId,
                r.ConsultancyPlanId, p != null ? p.Name : null,
                r.SchoolVisitId, v != null ? v.VisitDate : (DateOnly?)null,
                r.Title, r.Summary, r.FileId, r.AuthoredBy, r.CreatedAt)
        ).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Consultancy report {req.Id} not found.");
    }
}

// ── GetInstitutionReportQuery ─────────────────────────────────────────────────

/// <summary>
/// Institution Report: activity summary per institution (plans, visits, observations, reports).
/// Filterable by corporation and institution type.
/// </summary>
public class GetInstitutionReportQuery : IRequest<IReadOnlyList<InstitutionReportDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? InstitutionTypeId { get; set; }
}

public sealed class GetInstitutionReportQueryHandler
    : IRequestHandler<GetInstitutionReportQuery, IReadOnlyList<InstitutionReportDto>>
{
    private readonly IAppDbContext _db;

    public GetInstitutionReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<InstitutionReportDto>> Handle(
        GetInstitutionReportQuery req, CancellationToken ct)
    {
        var q = _db.Institutions.AsNoTracking()
            .Where(i => i.CorporationId == req.CorporationId && i.DeletedAt == null);

        if (req.InstitutionTypeId.HasValue)
            q = q.Where(i => i.InstitutionTypeId == req.InstitutionTypeId.Value);

        return await (
            from i in q
            join typ in _db.RefValues.AsNoTracking()
                on i.InstitutionTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new InstitutionReportDto(
                i.Id,
                i.Name,
                typ != null ? typ.Code : null,
                i.City,
                i.Plans.Count,
                i.Plans.Count(p => p.Status == "active"),
                i.Plans.Count(p => p.Status == "completed"),
                i.Visits.Count,
                i.Visits.Count(v => v.Status == "completed"),
                i.Visits.Sum(v => v.Observations.Count),
                i.Plans.Sum(p => p.Reports.Count) + i.Visits.Sum(v => v.Reports.Count(r => r.ConsultancyPlanId == null)))
        ).OrderBy(x => x.InstitutionName).ToListAsync(ct);
    }
}

// ── GetConsultancyOutcomesQuery ───────────────────────────────────────────────

/// <summary>
/// Consultancy Outcomes Report: plan-level summary with visit and observation counts.
/// </summary>
public class GetConsultancyOutcomesQuery : IRequest<IReadOnlyList<ConsultancyOutcomesDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? InstitutionId { get; set; }
    public string? Status { get; set; }
}

public sealed class GetConsultancyOutcomesQueryHandler
    : IRequestHandler<GetConsultancyOutcomesQuery, IReadOnlyList<ConsultancyOutcomesDto>>
{
    private readonly IAppDbContext _db;

    public GetConsultancyOutcomesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConsultancyOutcomesDto>> Handle(
        GetConsultancyOutcomesQuery req, CancellationToken ct)
    {
        var q = _db.ConsultancyPlans.AsNoTracking()
            .Where(p => p.CorporationId == req.CorporationId);

        if (req.InstitutionId.HasValue)
            q = q.Where(p => p.InstitutionId == req.InstitutionId.Value);
        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(p => p.Status == req.Status);

        return await (
            from p in q
            join i in _db.Institutions.AsNoTracking()
                on p.InstitutionId equals i.Id
            join typ in _db.RefValues.AsNoTracking()
                on p.ConsultancyTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new ConsultancyOutcomesDto(
                p.Id,
                p.Name,
                i.Name,
                typ != null ? typ.Code : null,
                p.PeriodStart,
                p.PeriodEnd,
                p.Status,
                p.Visits.Count,
                p.Visits.Count(v => v.Status == "completed"),
                p.Visits.Sum(v => v.Observations.Count),
                p.Reports.Count)
        ).OrderByDescending(x => x.PeriodStart).ToListAsync(ct);
    }
}

// ── GetVisitHistoryQuery ──────────────────────────────────────────────────────

/// <summary>
/// Visit History Report: chronological visit log with observation and report counts.
/// </summary>
public class GetVisitHistoryQuery : IRequest<IReadOnlyList<VisitHistoryItemDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? InstitutionId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public sealed class GetVisitHistoryQueryHandler
    : IRequestHandler<GetVisitHistoryQuery, IReadOnlyList<VisitHistoryItemDto>>
{
    private readonly IAppDbContext _db;

    public GetVisitHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<VisitHistoryItemDto>> Handle(
        GetVisitHistoryQuery req, CancellationToken ct)
    {
        var q = _db.SchoolVisits.AsNoTracking()
            .Where(v => v.CorporationId == req.CorporationId);

        if (req.InstitutionId.HasValue)
            q = q.Where(v => v.InstitutionId == req.InstitutionId.Value);
        if (req.DateFrom.HasValue)
            q = q.Where(v => v.VisitDate >= req.DateFrom.Value);
        if (req.DateTo.HasValue)
            q = q.Where(v => v.VisitDate <= req.DateTo.Value);

        return await (
            from v in q
            join i in _db.Institutions.AsNoTracking()
                on v.InstitutionId equals i.Id
            join p in _db.ConsultancyPlans.AsNoTracking()
                on v.ConsultancyPlanId equals p.Id into planGrp
            from p in planGrp.DefaultIfEmpty()
            select new VisitHistoryItemDto(
                v.Id,
                v.InstitutionId,
                i.Name,
                p != null ? p.Name : null,
                v.VisitDate,
                v.Purpose,
                v.Status,
                v.Observations.Count,
                v.Reports.Count,
                v.CreatedAt)
        ).OrderByDescending(x => x.VisitDate).ToListAsync(ct);
    }
}
