using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Corporations.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Corporations.Queries;

// ── Request ──────────────────────────────────────────────────────────────────
public record GetCorporationQuery(Guid Id) : IRequest<CorporationDto>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetCorporationQueryHandler : IRequestHandler<GetCorporationQuery, CorporationDto>
{
    private readonly IAppDbContext _db;

    public GetCorporationQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CorporationDto> Handle(GetCorporationQuery req, CancellationToken ct)
    {
        var corp = await _db.Corporations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Corporation", req.Id);

        var campusCount = await _db.Campuses
            .AsNoTracking()
            .CountAsync(c => c.CorporationId == req.Id, ct);

        return corp.ToDto(campusCount);
    }
}
