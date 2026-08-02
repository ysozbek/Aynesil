using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Queries;

// ── GetCampPeriodsQuery ───────────────────────────────────────────────────────

public record GetCampPeriodsQuery(Guid CampId) : IRequest<IReadOnlyList<CampPeriodListItemDto>>;

public sealed class GetCampPeriodsQueryHandler
    : IRequestHandler<GetCampPeriodsQuery, IReadOnlyList<CampPeriodListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetCampPeriodsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CampPeriodListItemDto>> Handle(
        GetCampPeriodsQuery req, CancellationToken ct)
    {
        return await (
            from p in _db.CampPeriods.AsNoTracking()
            where p.CampId == req.CampId
            orderby p.StartDate
            select new CampPeriodListItemDto(
                p.Id,
                p.CampId,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.Capacity,
                p.Enrollments.Count(e => e.Status == "enrolled"),
                p.Enrollments.Count(e => e.Status == "waitlist"))
        ).ToListAsync(ct);
    }
}

// ── GetCampPeriodQuery ────────────────────────────────────────────────────────

public record GetCampPeriodQuery(Guid Id) : IRequest<CampPeriodDto>;

public sealed class GetCampPeriodQueryHandler : IRequestHandler<GetCampPeriodQuery, CampPeriodDto>
{
    private readonly IAppDbContext _db;

    public GetCampPeriodQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CampPeriodDto> Handle(GetCampPeriodQuery req, CancellationToken ct)
    {
        var period = await _db.CampPeriods.AsNoTracking()
            .Include(p => p.Enrollments)
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camp period {req.Id} not found.");

        return new CampPeriodDto(
            period.Id, period.CampId, period.CorporationId,
            period.Name, period.StartDate, period.EndDate,
            period.Capacity,
            period.Enrollments.Count(e => e.Status == "enrolled"),
            period.Enrollments.Count(e => e.Status == "waitlist"));
    }
}
