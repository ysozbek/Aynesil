using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Campuses.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Campuses.Queries;

// ── Request ──────────────────────────────────────────────────────────────────
public record GetCampusQuery(Guid Id) : IRequest<CampusDto>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetCampusQueryHandler : IRequestHandler<GetCampusQuery, CampusDto>
{
    private readonly IAppDbContext _db;

    public GetCampusQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CampusDto> Handle(GetCampusQuery req, CancellationToken ct)
    {
        var campus = await _db.Campuses
            .AsNoTracking()
            .Include(c => c.Corporation)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Campus", req.Id);

        return campus.ToDto(campus.Corporation?.DisplayName ?? string.Empty);
    }
}
