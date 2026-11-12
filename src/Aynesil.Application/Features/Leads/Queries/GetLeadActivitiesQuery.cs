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

    private readonly ITenantContext _tenant;

    public GetLeadActivitiesQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResult<LeadActivityDto>> Handle(
        GetLeadActivitiesQuery req, CancellationToken ct)
    {
        var query =
            from a   in _db.LeadActivities
            join act in _db.RefValues    on a.ActivityTypeId equals act.Id into actG from act in actG.DefaultIfEmpty()
            join usr in _db.UserAccounts on a.PerformedBy    equals usr.Id into usrG from usr in usrG.DefaultIfEmpty()
            where a.LeadId == req.LeadId
            select new
            {
                a.Id,
                a.LeadId,
                a.ActivityTypeId,
                ActivityTypeCode = act == null ? null : act.Code,
                a.Subject,
                a.Body,
                a.Direction,
                a.OccurredAt,
                a.FollowUpAt,
                a.PerformedBy,
                PerformedByName = usr == null ? null : usr.FullName,
                a.CreatedAt
            };

        var projected = query
            .OrderByDescending(a => a.OccurredAt)
            .Select(r =>
                new LeadActivityDto(
                    r.Id,
                    r.LeadId,
                    r.ActivityTypeId,
                    r.ActivityTypeCode,
                    null,
                    r.Subject,
                    r.Body,
                    r.Direction,
                    r.OccurredAt,
                    r.FollowUpAt,
                    r.PerformedBy,
                    r.PerformedByName,
                    r.CreatedAt));

        var totalCount = await projected.CountAsync(ct);

        var items = await projected
            .Skip(req.Skip)
            .Take(req.PageSize)
            .ToListAsync(ct);

        items = await LeadLabels.ApplyAsync(_db, items, _tenant.Locale, ct);

        return PaginatedResult<LeadActivityDto>.Create(items, totalCount, req.Page, req.PageSize);
    }
}
