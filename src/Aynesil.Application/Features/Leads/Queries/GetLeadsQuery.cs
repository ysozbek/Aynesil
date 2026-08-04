using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leads.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Queries;

// ── Request ───────────────────────────────────────────────────────────────────
/// <summary>
/// Paginated, filterable list of leads.
/// Multi-corporation and multi-branch filters are both supported via CorporationId and CampusId.
/// PostgreSQL RLS provides the outer tenant boundary.
/// </summary>
public class GetLeadsQuery : PagedQuery, IRequest<PaginatedResult<LeadListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? PipelineStageId { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? AssignedToId { get; set; }
    public bool? IsConverted { get; set; }

    /// <summary>If true, returns only leads with an overdue follow-up activity.</summary>
    public bool HasPendingFollowUp { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetLeadsQueryHandler : IRequestHandler<GetLeadsQuery, PaginatedResult<LeadListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetLeadsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<LeadListItemDto>> Handle(GetLeadsQuery req, CancellationToken ct)
    {
        // Filter on the Lead entity first — EF cannot translate Where/OrderBy on a
        // custom record projected from a multi-join Select.
        var leads = _db.Leads.AsNoTracking();

        if (req.CorporationId.HasValue)
            leads = leads.Where(l => l.CorporationId == req.CorporationId.Value);

        if (req.CampusId.HasValue)
            leads = leads.Where(l => l.CampusId == req.CampusId.Value);

        if (req.StatusId.HasValue)
            leads = leads.Where(l => l.StatusId == req.StatusId.Value);

        if (req.PipelineStageId.HasValue)
            leads = leads.Where(l => l.PipelineStageId == req.PipelineStageId.Value);

        if (req.SourceId.HasValue)
            leads = leads.Where(l => l.SourceId == req.SourceId.Value);

        if (req.AssignedToId.HasValue)
            leads = leads.Where(l => l.AssignedToId == req.AssignedToId.Value);

        if (req.IsConverted.HasValue)
            leads = req.IsConverted.Value
                ? leads.Where(l => l.ConvertedStudentId != null)
                : leads.Where(l => l.ConvertedStudentId == null);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim();
            leads = leads.Where(l =>
                l.ContactName.Contains(search) ||
                (l.ChildName != null && l.ChildName.Contains(search)) ||
                (l.ContactPhone != null && l.ContactPhone.Contains(search)) ||
                (l.ContactEmail != null && l.ContactEmail.Contains(search)));
        }

        var joined =
            from l in leads
            join src in _db.RefValues.AsNoTracking() on l.SourceId equals src.Id into srcG
            from src in srcG.DefaultIfEmpty()
            join stat in _db.RefValues.AsNoTracking() on l.StatusId equals stat.Id into statG
            from stat in statG.DefaultIfEmpty()
            join stg in _db.RefValues.AsNoTracking() on l.PipelineStageId equals stg.Id into stgG
            from stg in stgG.DefaultIfEmpty()
            join camp in _db.Campuses.AsNoTracking() on l.CampusId equals camp.Id into campG
            from camp in campG.DefaultIfEmpty()
            join usr in _db.UserAccounts.AsNoTracking() on l.AssignedToId equals usr.Id into usrG
            from usr in usrG.DefaultIfEmpty()
            select new { l, src, stat, stg, camp, usr };

        var sorted = req.SortBy?.ToLowerInvariant() switch
        {
            "contactname"   => req.IsDescending ? joined.OrderByDescending(x => x.l.ContactName) : joined.OrderBy(x => x.l.ContactName),
            "childname"     => req.IsDescending ? joined.OrderByDescending(x => x.l.ChildName)   : joined.OrderBy(x => x.l.ChildName),
            "statuscode"    => req.IsDescending ? joined.OrderByDescending(x => x.stat!.Code)    : joined.OrderBy(x => x.stat!.Code),
            "pipelinestage" => req.IsDescending ? joined.OrderByDescending(x => x.stg!.Code)     : joined.OrderBy(x => x.stg!.Code),
            "createdat"     => req.IsDescending ? joined.OrderByDescending(x => x.l.CreatedAt)   : joined.OrderBy(x => x.l.CreatedAt),
            _               => joined.OrderByDescending(x => x.l.CreatedAt)
        };

        var totalCount = await sorted.CountAsync(ct);

        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(x => new LeadListItemDto(
                x.l.Id, x.l.CorporationId,
                x.l.CampusId, x.camp != null ? x.camp.Name : null,
                x.l.SourceId, x.src != null ? x.src.Code : null,
                x.l.StatusId, x.stat != null ? x.stat.Code : null,
                x.l.PipelineStageId, x.stg != null ? x.stg.Code : null,
                x.l.ChildName,
                x.l.ContactName, x.l.ContactPhone, x.l.ContactEmail,
                x.l.AssignedToId, x.usr != null ? x.usr.FullName : null,
                x.l.Score, x.l.ConvertedStudentId != null,
                x.l.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<LeadListItemDto>.Create(items, totalCount, req.Page, req.PageSize);
    }
}
