using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Queries;

// ── GetActivityParticipationsQuery ────────────────────────────────────────────

public class GetActivityParticipationsQuery
    : PagedQuery, IRequest<PaginatedResult<CampActivityParticipationDto>>
{
    public Guid? CampActivityId { get; set; }
    public Guid? CampEnrollmentId { get; set; }
    public string? Status { get; set; }
}

public sealed class GetActivityParticipationsQueryHandler
    : IRequestHandler<GetActivityParticipationsQuery, PaginatedResult<CampActivityParticipationDto>>
{
    private readonly IAppDbContext _db;

    public GetActivityParticipationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<CampActivityParticipationDto>> Handle(
        GetActivityParticipationsQuery req, CancellationToken ct)
    {
        var q = _db.CampActivityParticipations.AsNoTracking().AsQueryable();

        if (req.CampActivityId.HasValue)
            q = q.Where(p => p.CampActivityId == req.CampActivityId.Value);

        if (req.CampEnrollmentId.HasValue)
            q = q.Where(p => p.CampEnrollmentId == req.CampEnrollmentId.Value);

        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(p => p.Status == req.Status);

        var query = q.Select(p => new CampActivityParticipationDto(
            p.Id, p.CorporationId,
            p.CampActivityId, p.CampEnrollmentId,
            p.Status, p.Notes,
            p.RecordedBy, p.RecordedAt));

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "status" => req.IsDescending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _        => query.OrderByDescending(x => x.RecordedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<CampActivityParticipationDto>.Create(items, total, req.Page, req.PageSize);
    }
}
