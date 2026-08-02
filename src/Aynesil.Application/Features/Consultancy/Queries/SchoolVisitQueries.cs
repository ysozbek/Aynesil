using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Queries;

// ── GetSchoolVisitsQuery ──────────────────────────────────────────────────────

public class GetSchoolVisitsQuery
    : PagedQuery, IRequest<PaginatedResult<SchoolVisitListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? InstitutionId { get; set; }
    public Guid? ConsultancyPlanId { get; set; }
    public Guid? VisitorId { get; set; }
    public string? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public sealed class GetSchoolVisitsQueryHandler
    : IRequestHandler<GetSchoolVisitsQuery, PaginatedResult<SchoolVisitListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetSchoolVisitsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<SchoolVisitListItemDto>> Handle(
        GetSchoolVisitsQuery req, CancellationToken ct)
    {
        var q = _db.SchoolVisits.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(v => v.CorporationId == req.CorporationId.Value);
        if (req.InstitutionId.HasValue)
            q = q.Where(v => v.InstitutionId == req.InstitutionId.Value);
        if (req.ConsultancyPlanId.HasValue)
            q = q.Where(v => v.ConsultancyPlanId == req.ConsultancyPlanId.Value);
        if (req.VisitorId.HasValue)
            q = q.Where(v => v.VisitorId == req.VisitorId.Value);
        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(v => v.Status == req.Status);
        if (req.DateFrom.HasValue)
            q = q.Where(v => v.VisitDate >= req.DateFrom.Value);
        if (req.DateTo.HasValue)
            q = q.Where(v => v.VisitDate <= req.DateTo.Value);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(v => v.Purpose != null && v.Purpose.ToLower().Contains(term));
        }

        var query =
            from v in q
            join i in _db.Institutions.AsNoTracking()
                on v.InstitutionId equals i.Id
            join p in _db.ConsultancyPlans.AsNoTracking()
                on v.ConsultancyPlanId equals p.Id into planGrp
            from p in planGrp.DefaultIfEmpty()
            select new SchoolVisitListItemDto(
                v.Id,
                v.CorporationId,
                v.ConsultancyPlanId,
                p != null ? p.Name : null,
                v.InstitutionId,
                i.Name,
                v.VisitDate,
                v.VisitorId,
                v.Purpose,
                v.Status,
                v.Observations.Count,
                v.CreatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "visitdate"   => req.IsDescending ? query.OrderByDescending(x => x.VisitDate)   : query.OrderBy(x => x.VisitDate),
            "institution" => req.IsDescending ? query.OrderByDescending(x => x.InstitutionName) : query.OrderBy(x => x.InstitutionName),
            "status"      => req.IsDescending ? query.OrderByDescending(x => x.Status)     : query.OrderBy(x => x.Status),
            _             => query.OrderByDescending(x => x.VisitDate)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<SchoolVisitListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetSchoolVisitQuery ───────────────────────────────────────────────────────

public record GetSchoolVisitQuery(Guid Id) : IRequest<SchoolVisitDto>;

public sealed class GetSchoolVisitQueryHandler
    : IRequestHandler<GetSchoolVisitQuery, SchoolVisitDto>
{
    private readonly IAppDbContext _db;

    public GetSchoolVisitQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SchoolVisitDto> Handle(GetSchoolVisitQuery req, CancellationToken ct)
    {
        var visit = await _db.SchoolVisits.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"School visit {req.Id} not found.");

        var institutionName = await _db.Institutions.AsNoTracking()
            .Where(i => i.Id == visit.InstitutionId)
            .Select(i => i.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var planName = visit.ConsultancyPlanId.HasValue
            ? await _db.ConsultancyPlans.AsNoTracking()
                .Where(p => p.Id == visit.ConsultancyPlanId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var observations = await (
            from o in _db.ObservationRecords.AsNoTracking()
            where o.SchoolVisitId == req.Id
            join typ in _db.RefValues.AsNoTracking()
                on o.ObservationTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            orderby o.CreatedAt
            select new ObservationRecordDto(
                o.Id, o.CorporationId, o.SchoolVisitId,
                o.ObservationTypeId, typ != null ? typ.Code : null,
                o.Subject, o.Observation, o.Recommendations,
                o.CreatedAt, o.CreatedBy)
        ).ToListAsync(ct);

        return new SchoolVisitDto(
            visit.Id, visit.CorporationId,
            visit.ConsultancyPlanId, planName,
            visit.InstitutionId, institutionName,
            visit.VisitDate, visit.VisitorId, visit.Purpose, visit.Status,
            visit.CreatedAt, observations);
    }
}
