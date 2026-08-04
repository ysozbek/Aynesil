using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Programs.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Programs.Queries;

// ── GetProgramsQuery ──────────────────────────────────────────────────────────

public class GetProgramsQuery : PagedQuery, IRequest<PaginatedResult<ProgramListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? ProgramTypeId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetProgramsQueryHandler
    : IRequestHandler<GetProgramsQuery, PaginatedResult<ProgramListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetProgramsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ProgramListItemDto>> Handle(
        GetProgramsQuery req, CancellationToken ct)
    {
        var q = _db.EducationPrograms.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(p => p.CorporationId == req.CorporationId.Value);
        if (req.ProgramTypeId.HasValue)
            q = q.Where(p => p.ProgramTypeId == req.ProgramTypeId.Value);
        if (req.IsActive.HasValue)
            q = q.Where(p => p.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim().ToLower();
            q = q.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Code.ToLower().Contains(search));
        }

        var joined =
            from p in q
            join pt in _db.RefValues.AsNoTracking()
                on p.ProgramTypeId equals pt.Id into ptGrp
            from pt in ptGrp.DefaultIfEmpty()
            select new { p, pt };

        var sorted = req.SortBy?.ToLower() switch
        {
            "code"      => req.IsDescending ? joined.OrderByDescending(x => x.p.Code)      : joined.OrderBy(x => x.p.Code),
            "name"      => req.IsDescending ? joined.OrderByDescending(x => x.p.Name)      : joined.OrderBy(x => x.p.Name),
            "createdat" => req.IsDescending ? joined.OrderByDescending(x => x.p.CreatedAt) : joined.OrderBy(x => x.p.CreatedAt),
            _           => req.IsDescending ? joined.OrderByDescending(x => x.p.CreatedAt) : joined.OrderBy(x => x.p.Name)
        };

        var total = await sorted.CountAsync(ct);
        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(x => new ProgramListItemDto(
                x.p.Id, x.p.CorporationId, x.p.Code, x.p.Name,
                x.p.ProgramTypeId, x.pt != null ? x.pt.Code : null,
                x.p.Description, x.p.IsActive,
                x.p.Services.Count(),
                x.p.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<ProgramListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetProgramQuery ───────────────────────────────────────────────────────────

public record GetProgramQuery(Guid Id) : IRequest<ProgramDto>;

public sealed class GetProgramQueryHandler : IRequestHandler<GetProgramQuery, ProgramDto>
{
    private readonly IAppDbContext _db;

    public GetProgramQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ProgramDto> Handle(GetProgramQuery req, CancellationToken ct)
        => await ProgramProjection.LoadAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Program {req.Id} not found.");
}
