using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leads.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Queries;

// ── Request ───────────────────────────────────────────────────────────────────
/// <summary>
/// Returns a pipeline summary for the CRM dashboard:
/// count of leads per pipeline stage + totals for converted and lost leads.
/// </summary>
public record GetPipelineSummaryQuery(
    Guid? CampusId) : IRequest<PipelineSummaryDto>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetPipelineSummaryQueryHandler
    : IRequestHandler<GetPipelineSummaryQuery, PipelineSummaryDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPipelineSummaryQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PipelineSummaryDto> Handle(GetPipelineSummaryQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var leadsQuery = _db.Leads.Where(l => l.CorporationId == corporationId);

        if (req.CampusId.HasValue)
            leadsQuery = leadsQuery.Where(l => l.CampusId == req.CampusId.Value);

        var stageCounts = await (
            from l   in leadsQuery
            join stg in _db.RefValues on l.PipelineStageId equals stg.Id into stgG
            from stg in stgG.DefaultIfEmpty()
            where l.PipelineStageId != null
            group new { l, stg } by new { l.PipelineStageId, stg.Code } into g
            select new PipelineStageCountDto(
                g.Key.PipelineStageId!.Value,
                g.Key.Code,
                null,
                g.Count())
        ).ToListAsync(ct);

        stageCounts = await LeadLabels.ApplyAsync(_db, stageCounts, _tenant.Locale, ct);

        var totalLeads = await leadsQuery.CountAsync(ct);
        var totalConverted = await leadsQuery.CountAsync(l => l.ConvertedStudentId != null, ct);

        // 'lost' is the status code for lost leads — count by status code join
        var lostStatusId = await _db.RefValues
            .Where(rv => rv.Code == "lost" && rv.CorporationId == null)
            .Select(rv => (Guid?)rv.Id)
            .FirstOrDefaultAsync(ct);

        var totalLost = lostStatusId.HasValue
            ? await leadsQuery.CountAsync(l => l.StatusId == lostStatusId.Value, ct)
            : 0;

        return new PipelineSummaryDto(stageCounts, totalLeads, totalConverted, totalLost);
    }
}
