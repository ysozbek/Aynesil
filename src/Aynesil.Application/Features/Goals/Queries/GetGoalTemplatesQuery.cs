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

        var query =
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
            select new GoalTemplateListItemDto(
                t.Id, t.CorporationId,
                t.LibraryId, lib != null ? lib.Name : null,
                t.CategoryId, cat != null ? cat.Code : null,
                t.DevelopmentAreaId, dev != null ? dev.Code : null,
                t.Code, t.Statement, t.CreatedAt);

        query = req.SortBy?.ToLower() switch
        {
            "code"      => req.IsDescending ? query.OrderByDescending(t => t.Code) : query.OrderBy(t => t.Code),
            "createdat" => req.IsDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            _           => query.OrderBy(t => t.Code).ThenBy(t => t.Statement)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
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
