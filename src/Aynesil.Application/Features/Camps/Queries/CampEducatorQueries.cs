using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Queries;

// ── GetCampEducatorsQuery ─────────────────────────────────────────────────────

public class GetCampEducatorsQuery : IRequest<IReadOnlyList<CampEducatorDto>>
{
    public Guid? CampId { get; set; }
    public Guid? CampPeriodId { get; set; }
    public Guid? CampActivityId { get; set; }
    public Guid? EducatorId { get; set; }
}

public sealed class GetCampEducatorsQueryHandler
    : IRequestHandler<GetCampEducatorsQuery, IReadOnlyList<CampEducatorDto>>
{
    private readonly IAppDbContext _db;

    public GetCampEducatorsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CampEducatorDto>> Handle(
        GetCampEducatorsQuery req, CancellationToken ct)
    {
        var q = _db.CampEducators.AsNoTracking().AsQueryable();

        if (req.CampId.HasValue)
            q = q.Where(e => e.CampId == req.CampId.Value);

        if (req.CampPeriodId.HasValue)
            q = q.Where(e => e.CampPeriodId == req.CampPeriodId.Value
                          || e.CampPeriodId == null);

        if (req.CampActivityId.HasValue)
            q = q.Where(e => e.CampActivityId == req.CampActivityId.Value);

        if (req.EducatorId.HasValue)
            q = q.Where(e => e.EducatorId == req.EducatorId.Value);

        return await q
            .OrderBy(e => e.AssignedAt)
            .Select(e => new CampEducatorDto(
                e.Id, e.CorporationId, e.CampId,
                e.CampPeriodId, e.CampActivityId,
                e.EducatorId, e.Role,
                e.AssignedAt, e.AssignedBy))
            .ToListAsync(ct);
    }
}
