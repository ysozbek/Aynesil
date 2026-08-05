using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Queries;

// ── GetGoalTemplatesQuery ─────────────────────────────────────────────────────

public class GetGoalTemplatesQuery : PagedQuery, IRequest<PaginatedResult<GoalTemplateListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? LibraryId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? DevelopmentAreaId { get; set; }
}

public sealed class GetGoalTemplatesQueryHandler
    : IRequestHandler<GetGoalTemplatesQuery, PaginatedResult<GoalTemplateListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetGoalTemplatesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<GoalTemplateListItemDto>> Handle(
        GetGoalTemplatesQuery req, CancellationToken ct)
    {
        var q = _db.GoalTemplates.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(t => t.CorporationId == null || t.CorporationId == req.CorporationId.Value);

        if (req.LibraryId.HasValue)
            q = q.Where(t => t.LibraryId == req.LibraryId.Value);

        if (req.CategoryId.HasValue)
            q = q.Where(t => t.CategoryId == req.CategoryId.Value);

        if (req.DevelopmentAreaId.HasValue)
            q = q.Where(t => t.DevelopmentAreaId == req.DevelopmentAreaId.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(t => t.Statement.ToLower().Contains(s)
                           || (t.Code != null && t.Code.ToLower().Contains(s)));
        }

        // Sort on entity/join before DTO projection — EF cannot translate OrderBy on DTO ctor.
        var joined =
            from t in q
            join cat in _db.RefValues.AsNoTracking()
                on t.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            join dev in _db.RefValues.AsNoTracking()
                on t.DevelopmentAreaId equals dev.Id into devGrp
            from dev in devGrp.DefaultIfEmpty()
            join lib in _db.GoalLibraries.AsNoTracking()
                on t.LibraryId equals lib.Id into libGrp
            from lib in libGrp.DefaultIfEmpty()
            select new { t, cat, dev, lib };

        var sorted = req.SortBy?.ToLower() switch
        {
            "code"      => req.IsDescending ? joined.OrderByDescending(x => x.t.Code)      : joined.OrderBy(x => x.t.Code),
            "createdat" => req.IsDescending ? joined.OrderByDescending(x => x.t.CreatedAt) : joined.OrderBy(x => x.t.CreatedAt),
            _           => joined.OrderBy(x => x.t.Code).ThenBy(x => x.t.Statement)
        };

        var total = await sorted.CountAsync(ct);
        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(x => new GoalTemplateListItemDto(
                x.t.Id, x.t.CorporationId,
                x.t.LibraryId, x.lib != null ? x.lib.Name : null,
                x.t.CategoryId, x.cat != null ? x.cat.Code : null,
                x.t.DevelopmentAreaId, x.dev != null ? x.dev.Code : null,
                x.t.Code, x.t.Statement, x.t.CreatedAt))
            .ToListAsync(ct);
        return PaginatedResult<GoalTemplateListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetGoalTemplateQuery ──────────────────────────────────────────────────────

public record GetGoalTemplateQuery(Guid Id) : IRequest<GoalTemplateDto>;

public sealed class GetGoalTemplateQueryHandler : IRequestHandler<GetGoalTemplateQuery, GoalTemplateDto>
{
    private readonly IAppDbContext _db;

    public GetGoalTemplateQueryHandler(IAppDbContext db) => _db = db;

    public async Task<GoalTemplateDto> Handle(GetGoalTemplateQuery req, CancellationToken ct)
    {
        var t = await _db.GoalTemplates
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"GoalTemplate {req.Id} not found.");

        var catLabel = t.CategoryId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.CategoryId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var devAreaLabel = t.DevelopmentAreaId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.DevelopmentAreaId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var libraryName = t.LibraryId.HasValue
            ? await _db.GoalLibraries.AsNoTracking()
                .Where(l => l.Id == t.LibraryId.Value).Select(l => l.Name).FirstOrDefaultAsync(ct)
            : null;

        return new GoalTemplateDto(
            t.Id, t.CorporationId, t.LibraryId, libraryName,
            t.CategoryId, catLabel, t.DevelopmentAreaId, devAreaLabel,
            t.Code, t.Statement, t.DefaultCriteria,
            t.CreatedAt, t.UpdatedAt, t.RowVersion,
            t.Translations.Select(GoalProjection.ToTranslationDto).ToList());
    }
}
