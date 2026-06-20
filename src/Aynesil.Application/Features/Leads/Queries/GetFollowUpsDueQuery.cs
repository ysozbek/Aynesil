using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leads.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Queries;

// ── Request ───────────────────────────────────────────────────────────────────
/// <summary>
/// Returns activities that have a scheduled follow-up on or before <see cref="DueBy"/>.
/// Used to render the "Today's follow-ups" dashboard widget.
/// </summary>
public class GetFollowUpsDueQuery : PagedQuery, IRequest<PaginatedResult<LeadActivityDto>>
{
    public required Guid CorporationId { get; set; }
    public Guid? CampusId { get; set; }

    /// <summary>Upper bound for follow-up date. Defaults to the current moment (i.e. overdue + due today).</summary>
    public DateTimeOffset? DueBy { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetFollowUpsDueQueryHandler
    : IRequestHandler<GetFollowUpsDueQuery, PaginatedResult<LeadActivityDto>>
{
    private readonly IAppDbContext _db;

    public GetFollowUpsDueQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<LeadActivityDto>> Handle(
        GetFollowUpsDueQuery req, CancellationToken ct)
    {
        var dueBy = req.DueBy ?? DateTimeOffset.UtcNow;

        var query =
            from a    in _db.LeadActivities
            join l    in _db.Leads        on a.LeadId         equals l.Id   // join for campus filter
            join act  in _db.RefValues    on a.ActivityTypeId equals act.Id into actG from act in actG.DefaultIfEmpty()
            join usr  in _db.UserAccounts on a.PerformedBy    equals usr.Id into usrG from usr in usrG.DefaultIfEmpty()
            where a.CorporationId == req.CorporationId
               && a.FollowUpAt != null
               && a.FollowUpAt <= dueBy
               && (!req.CampusId.HasValue || l.CampusId == req.CampusId)
            select new LeadActivityDto(
                a.Id, a.LeadId,
                a.ActivityTypeId, act == null ? null : act.Code,
                a.Subject, a.Body, a.Direction,
                a.OccurredAt, a.FollowUpAt,
                a.PerformedBy, usr == null ? null : usr.FullName,
                a.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.FollowUpAt)
            .Skip(req.Skip)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return PaginatedResult<LeadActivityDto>.Create(items, totalCount, req.Page, req.PageSize);
    }
}
