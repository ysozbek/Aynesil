using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.RefData;
using Aynesil.Application.Features.Leads.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Queries;

// ── Request ───────────────────────────────────────────────────────────────────
/// <summary>
/// Conversion-rate report: total vs converted lead counts for a date window,
/// broken down by lead source. Used by the CRM analytics screen.
/// </summary>
public record GetConversionReportQuery(
    DateTimeOffset From,
    DateTimeOffset To,
    Guid? CampusId) : IRequest<ConversionReportDto>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetConversionReportQueryHandler
    : IRequestHandler<GetConversionReportQuery, ConversionReportDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetConversionReportQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ConversionReportDto> Handle(GetConversionReportQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var leadsQuery = _db.Leads
            .Where(l => l.CorporationId == corporationId
                     && l.CreatedAt >= req.From
                     && l.CreatedAt <= req.To);

        if (req.CampusId.HasValue)
            leadsQuery = leadsQuery.Where(l => l.CampusId == req.CampusId.Value);

        var bySource = await (
            from l   in leadsQuery
            join src in _db.RefValues on l.SourceId equals src.Id into srcG
            from src in srcG.DefaultIfEmpty()
            group new { l, src } by new { l.SourceId, SourceCode = src == null ? null : src.Code } into g
            select new ConversionBySourceDto(
                g.Key.SourceId,
                g.Key.SourceCode,
                null,
                g.Count(),
                g.Count(x => x.l.ConvertedStudentId != null),
                g.Count() > 0
                    ? Math.Round((decimal)g.Count(x => x.l.ConvertedStudentId != null) / g.Count() * 100, 1)
                    : 0m)
        ).ToListAsync(ct);

        var sourceIds = bySource.Where(s => s.SourceId.HasValue).Select(s => s.SourceId!.Value).Distinct().ToList();
        var sourceNames = await RefLabelLoader.LoadAsync(_db, sourceIds, _tenant.Locale, ct);
        bySource = bySource
            .Select(s => s.SourceId.HasValue ? s with { SourceName = sourceNames.GetValueOrDefault(s.SourceId.Value) } : s)
            .ToList();

        var totalLeads = bySource.Sum(s => s.TotalLeads);
        var totalConverted = bySource.Sum(s => s.Converted);
        var conversionRate = totalLeads > 0
            ? Math.Round((decimal)totalConverted / totalLeads * 100, 1)
            : 0m;

        return new ConversionReportDto(
            req.From, req.To,
            totalLeads, totalConverted, conversionRate,
            bySource);
    }
}
