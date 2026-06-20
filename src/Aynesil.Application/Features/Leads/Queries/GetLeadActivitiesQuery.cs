using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leads.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Queries;

// ── Request ───────────────────────────────────────────────────────────────────
public class GetLeadActivitiesQuery : PagedQuery, IRequest<PaginatedResult<LeadActivityDto>>
{
    public required Guid LeadId { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetLeadActivitiesQueryHandler
    : IRequestHandler<GetLeadActivitiesQuery, PaginatedResult<LeadActivityDto>>
{
    private readonly IAppDbContext _db;

    public GetLeadActivitiesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<LeadActivityDto>> Handle(
        GetLeadActivitiesQuery req, CancellationToken ct)
    {
        var query =
            from a   in _db.LeadActivities
            join act in _db.RefValues    on a.ActivityTypeId equals act.Id into actG from act in actG.DefaultIfEmpty()
            join usr in _db.UserAccounts on a.PerformedBy    equals usr.Id into usrG from usr in usrG.DefaultIfEmpty()
            where a.LeadId == req.LeadId
            select new LeadActivityDto(
                a.Id, a.LeadId,
                a.ActivityTypeId, act == null ? null : act.Code,
                a.Subject, a.Body, a.Direction,
                a.OccurredAt, a.FollowUpAt,
                a.PerformedBy, usr == null ? null : usr.FullName,
                a.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip(req.Skip)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return PaginatedResult<LeadActivityDto>.Create(items, totalCount, req.Page, req.PageSize);
    }
}
