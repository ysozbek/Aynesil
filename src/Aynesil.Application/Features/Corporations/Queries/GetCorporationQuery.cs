using Aynesil.Domain.Common;
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
    private readonly ITenantContext _tenant;

    public GetCorporationQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<CorporationDto> Handle(GetCorporationQuery req, CancellationToken ct)
    {
        // Only the caller's own corporation; any other id is "not found" (Y1). RLS enforces the same.
        var corporationId = _tenant.RequireCorporationId();

        var corp = await _db.Corporations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.Id == corporationId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Corporation"));

        var campusCount = await _db.Campuses
            .AsNoTracking()
            .CountAsync(c => c.CorporationId == req.Id, ct);

        return corp.ToDto(campusCount);
    }
}
