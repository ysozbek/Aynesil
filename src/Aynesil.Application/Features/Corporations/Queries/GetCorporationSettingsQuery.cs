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

    public GetCorporationSettingsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CorporationSettingsDto> Handle(GetCorporationSettingsQuery req, CancellationToken ct)
    {
        var corp = await _db.Corporations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Corporation", req.Id);

        return corp.ToSettingsDto();
    }
}
