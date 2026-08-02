using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.PerformanceKpi.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.PerformanceKpi.Queries;

// ── GetEducatorKpiValuesQuery ─────────────────────────────────────────────────

/// <summary>
/// Returns all computed KPI values for a single educator, optionally filtered
/// by period and KPI code. Joins kpi_definition for code/name/unit.
/// </summary>
public class GetEducatorKpiValuesQuery : IRequest<IReadOnlyList<KpiValueDto>>
{
    public Guid CorporationId { get; set; }
    public Guid EducatorId { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }

    /// <summary>Filter by KPI code prefix, e.g. "educator." to get all educator KPIs.</summary>
    public string? KpiCodePrefix { get; set; }
}

public sealed class GetEducatorKpiValuesQueryHandler
    : IRequestHandler<GetEducatorKpiValuesQuery, IReadOnlyList<KpiValueDto>>
{
    private readonly IAppDbContext _db;

    public GetEducatorKpiValuesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<KpiValueDto>> Handle(
        GetEducatorKpiValuesQuery req, CancellationToken ct)
    {
        var q =
            from kv in _db.KpiValues.AsNoTracking()
            join kd in _db.KpiDefinitions.AsNoTracking() on kv.KpiId equals kd.Id
            where kv.CorporationId == req.CorporationId
               && kv.SubjectType == "educator"
               && kv.SubjectId == req.EducatorId
            select new { kv, kd };

        if (req.PeriodStart.HasValue)
            q = q.Where(x => x.kv.PeriodStart >= req.PeriodStart.Value);

        if (req.PeriodEnd.HasValue)
            q = q.Where(x => x.kv.PeriodEnd <= req.PeriodEnd.Value);

        if (!string.IsNullOrWhiteSpace(req.KpiCodePrefix))
        {
            var prefix = req.KpiCodePrefix.Trim().ToLower();
            q = q.Where(x => x.kd.Code.StartsWith(prefix));
        }

        return await q
            .OrderBy(x => x.kv.PeriodStart)
            .ThenBy(x => x.kd.Code)
            .Select(x => new KpiValueDto(
                x.kv.Id, x.kv.CorporationId, x.kv.KpiId,
                x.kd.Code, x.kd.Name, x.kd.Unit,
                x.kv.SubjectType, x.kv.SubjectId,
                x.kv.PeriodStart, x.kv.PeriodEnd,
                x.kv.NumericValue, x.kv.ComputedAt))
            .ToListAsync(ct);
    }
}

// ── GetKpiValuesQuery (generic, paginated) ────────────────────────────────────

public class GetKpiValuesQuery : PagedQuery, IRequest<PaginatedResult<KpiValueDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? KpiId { get; set; }
    public string? SubjectType { get; set; }
    public Guid? SubjectId { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
}

public sealed class GetKpiValuesQueryHandler
    : IRequestHandler<GetKpiValuesQuery, PaginatedResult<KpiValueDto>>
{
    private readonly IAppDbContext _db;

    public GetKpiValuesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<KpiValueDto>> Handle(
        GetKpiValuesQuery req, CancellationToken ct)
    {
        var q =
            from kv in _db.KpiValues.AsNoTracking()
            join kd in _db.KpiDefinitions.AsNoTracking() on kv.KpiId equals kd.Id
            where kv.CorporationId == req.CorporationId
            select new { kv, kd };

        if (req.KpiId.HasValue)
            q = q.Where(x => x.kv.KpiId == req.KpiId.Value);

        if (!string.IsNullOrWhiteSpace(req.SubjectType))
            q = q.Where(x => x.kv.SubjectType == req.SubjectType);

        if (req.SubjectId.HasValue)
            q = q.Where(x => x.kv.SubjectId == req.SubjectId.Value);

        if (req.PeriodStart.HasValue)
            q = q.Where(x => x.kv.PeriodStart >= req.PeriodStart.Value);

        if (req.PeriodEnd.HasValue)
            q = q.Where(x => x.kv.PeriodEnd <= req.PeriodEnd.Value);

        var projection = q.Select(x => new KpiValueDto(
            x.kv.Id, x.kv.CorporationId, x.kv.KpiId,
            x.kd.Code, x.kd.Name, x.kd.Unit,
            x.kv.SubjectType, x.kv.SubjectId,
            x.kv.PeriodStart, x.kv.PeriodEnd,
            x.kv.NumericValue, x.kv.ComputedAt));

        projection = req.SortBy?.ToLowerInvariant() switch
        {
            "code"        => req.IsDescending ? projection.OrderByDescending(x => x.KpiCode)   : projection.OrderBy(x => x.KpiCode),
            "period"      => req.IsDescending ? projection.OrderByDescending(x => x.PeriodStart): projection.OrderBy(x => x.PeriodStart),
            "value"       => req.IsDescending ? projection.OrderByDescending(x => x.NumericValue): projection.OrderBy(x => x.NumericValue),
            "computedat"  => req.IsDescending ? projection.OrderByDescending(x => x.ComputedAt) : projection.OrderBy(x => x.ComputedAt),
            _             => projection.OrderByDescending(x => x.PeriodStart).ThenBy(x => x.KpiCode)
        };

        var total = await projection.CountAsync(ct);
        var items = await projection.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<KpiValueDto>.Create(items, total, req.Page, req.PageSize);
    }
}
