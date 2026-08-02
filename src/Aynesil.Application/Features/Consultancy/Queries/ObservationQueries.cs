using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Queries;

// ── GetObservationsQuery ──────────────────────────────────────────────────────

public record GetObservationsQuery(Guid SchoolVisitId)
    : IRequest<IReadOnlyList<ObservationRecordDto>>;

public sealed class GetObservationsQueryHandler
    : IRequestHandler<GetObservationsQuery, IReadOnlyList<ObservationRecordDto>>
{
    private readonly IAppDbContext _db;

    public GetObservationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ObservationRecordDto>> Handle(
        GetObservationsQuery req, CancellationToken ct)
    {
        return await (
            from o in _db.ObservationRecords.AsNoTracking()
            where o.SchoolVisitId == req.SchoolVisitId
            join typ in _db.RefValues.AsNoTracking()
                on o.ObservationTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            orderby o.CreatedAt
            select new ObservationRecordDto(
                o.Id, o.CorporationId, o.SchoolVisitId,
                o.ObservationTypeId, typ != null ? typ.Code : null,
                o.Subject, o.Observation, o.Recommendations,
                o.CreatedAt, o.CreatedBy)
        ).ToListAsync(ct);
    }
}

// ── GetObservationQuery ───────────────────────────────────────────────────────

public record GetObservationQuery(Guid Id) : IRequest<ObservationRecordDto>;

public sealed class GetObservationQueryHandler
    : IRequestHandler<GetObservationQuery, ObservationRecordDto>
{
    private readonly IAppDbContext _db;

    public GetObservationQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ObservationRecordDto> Handle(
        GetObservationQuery req, CancellationToken ct)
    {
        return await (
            from o in _db.ObservationRecords.AsNoTracking()
            where o.Id == req.Id
            join typ in _db.RefValues.AsNoTracking()
                on o.ObservationTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new ObservationRecordDto(
                o.Id, o.CorporationId, o.SchoolVisitId,
                o.ObservationTypeId, typ != null ? typ.Code : null,
                o.Subject, o.Observation, o.Recommendations,
                o.CreatedAt, o.CreatedBy)
        ).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Observation {req.Id} not found.");
    }
}
