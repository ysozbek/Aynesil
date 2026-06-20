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
        var query = LeadProjection.BuildListQuery(_db);

        if (req.CorporationId.HasValue)
            query = query.Where(l => l.CorporationId == req.CorporationId.Value);

        if (req.CampusId.HasValue)
            query = query.Where(l => l.CampusId == req.CampusId.Value);

        if (req.StatusId.HasValue)
            query = query.Where(l => l.StatusId == req.StatusId.Value);

        if (req.PipelineStageId.HasValue)
            query = query.Where(l => l.PipelineStageId == req.PipelineStageId.Value);

        if (req.SourceId.HasValue)
            query = query.Where(l => l.SourceId == req.SourceId.Value);

        if (req.AssignedToId.HasValue)
            query = query.Where(l => l.AssignedToId == req.AssignedToId.Value);

        if (req.IsConverted.HasValue)
            query = query.Where(l => l.IsConverted == req.IsConverted.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(l =>
                l.ContactName.Contains(req.Search) ||
                (l.ChildName != null && l.ChildName.Contains(req.Search)) ||
                (l.ContactPhone != null && l.ContactPhone.Contains(req.Search)) ||
                (l.ContactEmail != null && l.ContactEmail.Contains(req.Search)));

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "contactname"   => req.IsDescending ? query.OrderByDescending(l => l.ContactName)   : query.OrderBy(l => l.ContactName),
            "childname"     => req.IsDescending ? query.OrderByDescending(l => l.ChildName)     : query.OrderBy(l => l.ChildName),
            "statuscode"    => req.IsDescending ? query.OrderByDescending(l => l.StatusCode)    : query.OrderBy(l => l.StatusCode),
            "pipelinestage" => req.IsDescending ? query.OrderByDescending(l => l.PipelineStageCode) : query.OrderBy(l => l.PipelineStageCode),
            "createdat"     => req.IsDescending ? query.OrderByDescending(l => l.CreatedAt)     : query.OrderBy(l => l.CreatedAt),
            _               => query.OrderByDescending(l => l.CreatedAt)
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip(req.Skip)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return PaginatedResult<LeadListItemDto>.Create(items, totalCount, req.Page, req.PageSize);
    }
}
