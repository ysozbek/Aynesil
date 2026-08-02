using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.PerformanceKpi.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.PerformanceKpi.Queries;

// ── GetEducatorKpiTrendQuery ──────────────────────────────────────────────────

/// <summary>
/// Historical KPI trend for a single educator across multiple periods.
/// Returns up to <c>MaxPeriods</c> data points (oldest→newest) from kpi_value.
/// </summary>
public record GetEducatorKpiTrendQuery(
    Guid CorporationId,
    Guid EducatorId,
    string KpiCode,
    DateOnly? FromPeriodStart = null,
    int MaxPeriods = 12) : IRequest<KpiTrendDto>;

public sealed class GetEducatorKpiTrendQueryHandler
    : IRequestHandler<GetEducatorKpiTrendQuery, KpiTrendDto>
{
    private readonly IAppDbContext _db;

    public GetEducatorKpiTrendQueryHandler(IAppDbContext db) => _db = db;

    public async Task<KpiTrendDto> Handle(
        GetEducatorKpiTrendQuery req, CancellationToken ct)
    {
        var kpiDef = await _db.KpiDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Code == req.KpiCode, ct)
            ?? throw new KeyNotFoundException(
                $"KPI definition with code '{req.KpiCode}' not found.");

        var q = _db.KpiValues
            .AsNoTracking()
            .Where(kv => kv.CorporationId == req.CorporationId
                      && kv.KpiId == kpiDef.Id
                      && kv.SubjectType == "educator"
                      && kv.SubjectId == req.EducatorId);

        if (req.FromPeriodStart.HasValue)
            q = q.Where(kv => kv.PeriodStart >= req.FromPeriodStart.Value);

        var points = await q
            .OrderByDescending(kv => kv.PeriodEnd)
            .Take(req.MaxPeriods)
            .OrderBy(kv => kv.PeriodStart)
            .Select(kv => new TrendPointDto(
                kv.PeriodStart, kv.PeriodEnd,
                kv.PeriodStart.ToString("MMM yyyy"),
                kv.NumericValue))
            .ToListAsync(ct);

        return new KpiTrendDto(kpiDef.Code, kpiDef.Name, kpiDef.Unit, points);
    }
}

// ── GetCorporationKpiTrendQuery ───────────────────────────────────────────────

/// <summary>
/// Corporation-wide average trend for a KPI across multiple periods.
/// Each data point is the average NumericValue across all educator kpi_value rows for that period.
/// </summary>
public record GetCorporationKpiTrendQuery(
    Guid CorporationId,
    string KpiCode,
    DateOnly? FromPeriodStart = null,
    int MaxPeriods = 12) : IRequest<KpiTrendDto>;

public sealed class GetCorporationKpiTrendQueryHandler
    : IRequestHandler<GetCorporationKpiTrendQuery, KpiTrendDto>
{
    private readonly IAppDbContext _db;

    public GetCorporationKpiTrendQueryHandler(IAppDbContext db) => _db = db;

    public async Task<KpiTrendDto> Handle(
        GetCorporationKpiTrendQuery req, CancellationToken ct)
    {
        var kpiDef = await _db.KpiDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Code == req.KpiCode, ct)
            ?? throw new KeyNotFoundException(
                $"KPI definition with code '{req.KpiCode}' not found.");

        var q = _db.KpiValues
            .AsNoTracking()
            .Where(kv => kv.CorporationId == req.CorporationId
                      && kv.KpiId == kpiDef.Id
                      && kv.SubjectType == "educator"
                      && kv.NumericValue.HasValue);

        if (req.FromPeriodStart.HasValue)
            q = q.Where(kv => kv.PeriodStart >= req.FromPeriodStart.Value);

        // Group by period, average, take last N periods
        var raw = await q
            .GroupBy(kv => new { kv.PeriodStart, kv.PeriodEnd })
            .Select(g => new
            {
                g.Key.PeriodStart,
                g.Key.PeriodEnd,
                AvgValue = g.Average(kv => kv.NumericValue)
            })
            .OrderByDescending(x => x.PeriodEnd)
            .Take(req.MaxPeriods)
            .ToListAsync(ct);

        var points = raw
            .OrderBy(x => x.PeriodStart)
            .Select(x => new TrendPointDto(
                x.PeriodStart, x.PeriodEnd,
                x.PeriodStart.ToString("MMM yyyy"),
                x.AvgValue.HasValue ? Math.Round(x.AvgValue.Value, 2) : null))
            .ToList();

        return new KpiTrendDto(kpiDef.Code, kpiDef.Name, kpiDef.Unit, points);
    }
}

// ── GetEducatorRankingQuery ───────────────────────────────────────────────────

/// <summary>
/// Educator ranking by a specific KPI for a given period.
/// Returns all educators with a computed value, ranked highest→lowest (or lowest→highest
/// when <c>RankAscending</c> = true, e.g. for error rates).
/// </summary>
public record GetEducatorRankingQuery(
    Guid CorporationId,
    string KpiCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    Guid? CampusId = null,
    bool RankAscending = false) : IRequest<IReadOnlyList<RankingItemDto>>;

public sealed class GetEducatorRankingQueryHandler
    : IRequestHandler<GetEducatorRankingQuery, IReadOnlyList<RankingItemDto>>
{
    private readonly IAppDbContext _db;

    public GetEducatorRankingQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<RankingItemDto>> Handle(
        GetEducatorRankingQuery req, CancellationToken ct)
    {
        var kpiDef = await _db.KpiDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Code == req.KpiCode, ct)
            ?? throw new KeyNotFoundException(
                $"KPI definition with code '{req.KpiCode}' not found.");

        var kpiValues = await _db.KpiValues
            .AsNoTracking()
            .Where(kv => kv.CorporationId == req.CorporationId
                      && kv.KpiId == kpiDef.Id
                      && kv.SubjectType == "educator"
                      && kv.SubjectId.HasValue
                      && kv.PeriodStart == req.PeriodStart
                      && kv.PeriodEnd == req.PeriodEnd
                      && kv.NumericValue.HasValue)
            .Select(kv => new { EducatorId = kv.SubjectId!.Value, kv.NumericValue })
            .ToListAsync(ct);

        // Optionally filter by campus
        if (req.CampusId.HasValue)
        {
            var campusEducatorIds = await _db.EducatorCampuses
                .AsNoTracking()
                .Where(ec => ec.CampusId == req.CampusId.Value)
                .Select(ec => ec.EducatorId)
                .ToListAsync(ct);

            kpiValues = kpiValues
                .Where(kv => campusEducatorIds.Contains(kv.EducatorId))
                .ToList();
        }

        var educatorIds = kpiValues.Select(kv => kv.EducatorId).ToList();
        var educators = await _db.Educators
            .AsNoTracking()
            .Where(e => educatorIds.Contains(e.Id) && e.DeletedAt == null)
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.TitleId })
            .ToDictionaryAsync(e => e.Id, ct);

        var sorted = req.RankAscending
            ? kpiValues.OrderBy(kv => kv.NumericValue).ToList()
            : kpiValues.OrderByDescending(kv => kv.NumericValue).ToList();

        return sorted
            .Select((kv, idx) =>
            {
                educators.TryGetValue(kv.EducatorId, out var ed);
                return new RankingItemDto(
                    Rank: idx + 1,
                    EducatorId: kv.EducatorId,
                    FullName: ed is not null ? $"{ed.FirstName} {ed.LastName}" : kv.EducatorId.ToString(),
                    TitleCode: null,
                    KpiValue: kv.NumericValue,
                    KpiCode: kpiDef.Code,
                    KpiName: kpiDef.Name,
                    Unit: kpiDef.Unit);
            })
            .ToList();
    }
}
