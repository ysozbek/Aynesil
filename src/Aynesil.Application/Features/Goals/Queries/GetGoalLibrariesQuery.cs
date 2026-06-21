using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Queries;

// ── GetGoalLibrariesQuery ─────────────────────────────────────────────────────

public class GetGoalLibrariesQuery : PagedQuery, IRequest<PaginatedResult<GoalLibraryListItemDto>>
{
    /// <summary>Null returns platform + tenant libraries. Pass a specific ID to filter by tenant.</summary>
    public Guid? CorporationId { get; set; }
}

public sealed class GetGoalLibrariesQueryHandler
    : IRequestHandler<GetGoalLibrariesQuery, PaginatedResult<GoalLibraryListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetGoalLibrariesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<GoalLibraryListItemDto>> Handle(
        GetGoalLibrariesQuery req, CancellationToken ct)
    {
        var q = _db.GoalLibraries.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(l => l.CorporationId == null || l.CorporationId == req.CorporationId.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(l => l.Name.ToLower().Contains(s));
        }

        var query =
            from l in q
            let templateCount = _db.GoalTemplates.Count(t => t.LibraryId == l.Id)
            select new GoalLibraryListItemDto(
                l.Id, l.CorporationId, l.Name, l.Description,
                templateCount, l.CreatedAt);

        query = req.SortBy?.ToLower() switch
        {
            "name"      => req.IsDescending ? query.OrderByDescending(l => l.Name) : query.OrderBy(l => l.Name),
            "createdat" => req.IsDescending ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt),
            _           => query.OrderBy(l => l.Name)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<GoalLibraryListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetGoalLibraryQuery ───────────────────────────────────────────────────────

public record GetGoalLibraryQuery(Guid Id) : IRequest<GoalLibraryDto>;

public sealed class GetGoalLibraryQueryHandler : IRequestHandler<GetGoalLibraryQuery, GoalLibraryDto>
{
    private readonly IAppDbContext _db;

    public GetGoalLibraryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<GoalLibraryDto> Handle(GetGoalLibraryQuery req, CancellationToken ct)
    {
        var library = await _db.GoalLibraries.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"GoalLibrary {req.Id} not found.");

        var templateCount = await _db.GoalTemplates.CountAsync(
            t => t.LibraryId == library.Id, ct);

        return new GoalLibraryDto(
            library.Id, library.CorporationId, library.Name, library.Description,
            templateCount, library.CreatedAt, library.UpdatedAt, library.RowVersion);
    }
}
