using Aynesil.Domain.Common;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Corporations.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Corporations.Queries;

// ── Request ──────────────────────────────────────────────────────────────────
/// <summary>Returns the settings blob and locale/currency/timezone preferences for a corporation.</summary>
public record GetCorporationSettingsQuery(Guid Id) : IRequest<CorporationSettingsDto>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetCorporationSettingsQueryHandler
    : IRequestHandler<GetCorporationSettingsQuery, CorporationSettingsDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetCorporationSettingsQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<CorporationSettingsDto> Handle(GetCorporationSettingsQuery req, CancellationToken ct)
    {
        // Only the caller's own corporation; any other id is "not found" (Y1). RLS enforces the same.
        var corporationId = _tenant.RequireCorporationId();

        var corp = await _db.Corporations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.Id == corporationId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Corporation"));

        return corp.ToSettingsDto();
    }
}
