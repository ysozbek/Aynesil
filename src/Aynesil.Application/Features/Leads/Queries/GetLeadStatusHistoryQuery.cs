using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leads.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Queries;

// ── Request ───────────────────────────────────────────────────────────────────
public record GetLeadStatusHistoryQuery(Guid LeadId) : IRequest<IReadOnlyList<LeadStatusHistoryDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetLeadStatusHistoryQueryHandler
    : IRequestHandler<GetLeadStatusHistoryQuery, IReadOnlyList<LeadStatusHistoryDto>>
{
    private readonly IAppDbContext _db;

    public GetLeadStatusHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeadStatusHistoryDto>> Handle(
        GetLeadStatusHistoryQuery req, CancellationToken ct)
    {
        var rows = await (
            from h    in _db.LeadStatusHistories
            join stat in _db.RefValues on h.StatusId        equals stat.Id into statG from stat in statG.DefaultIfEmpty()
            join stg  in _db.RefValues on h.PipelineStageId equals stg.Id  into stgG  from stg  in stgG.DefaultIfEmpty()
            where h.LeadId == req.LeadId
            orderby h.ChangedAt descending
            select new LeadStatusHistoryDto(
                h.Id,
                h.StatusId, stat == null ? null : stat.Code,
                h.PipelineStageId, stg == null ? null : stg.Code,
                h.ChangedAt, h.ChangedBy)
        ).ToListAsync(ct);

        return rows;
    }
}
