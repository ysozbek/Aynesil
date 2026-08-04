using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Notifications.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Notifications.Queries;

// ── GetNotificationTemplatesQuery ─────────────────────────────────────────────

public class GetNotificationTemplatesQuery
    : PagedQuery, IRequest<PaginatedResult<NotificationTemplateListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public bool? IsActive { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? TypeId { get; set; }
}

public sealed class GetNotificationTemplatesQueryHandler
    : IRequestHandler<GetNotificationTemplatesQuery, PaginatedResult<NotificationTemplateListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetNotificationTemplatesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<NotificationTemplateListItemDto>> Handle(
        GetNotificationTemplatesQuery req, CancellationToken ct)
    {
        // Filter/sort on entity first — EF cannot translate Where/OrderBy on DTO projection.
        var templates = _db.NotificationTemplates.AsNoTracking();

        if (req.CorporationId.HasValue)
            templates = templates.Where(t => t.CorporationId == req.CorporationId.Value || t.CorporationId == null);

        if (req.IsActive.HasValue)
            templates = templates.Where(t => t.IsActive == req.IsActive.Value);

        if (req.CategoryId.HasValue)
            templates = templates.Where(t => t.CategoryId == req.CategoryId.Value);

        if (req.TypeId.HasValue)
            templates = templates.Where(t => t.TypeId == req.TypeId.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            templates = templates.Where(t => t.Code.ToLower().Contains(s));
        }

        var joined =
            from t in templates
            join cat in _db.RefValues.AsNoTracking()
                on t.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            join typ in _db.RefValues.AsNoTracking()
                on t.TypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new { t, cat, typ };

        var sorted = req.SortBy?.ToLowerInvariant() switch
        {
            "code"      => req.IsDescending ? joined.OrderByDescending(x => x.t.Code)      : joined.OrderBy(x => x.t.Code),
            "updatedat" => req.IsDescending ? joined.OrderByDescending(x => x.t.UpdatedAt) : joined.OrderBy(x => x.t.UpdatedAt),
            _           => joined.OrderBy(x => x.t.Code)
        };

        var total = await sorted.CountAsync(ct);
        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(x => new NotificationTemplateListItemDto(
                x.t.Id, x.t.CorporationId, x.t.Code,
                x.cat != null ? x.cat.Code : null,
                x.typ != null ? x.typ.Code : null,
                x.t.IsActive, x.t.UpdatedAt))
            .ToListAsync(ct);
        return PaginatedResult<NotificationTemplateListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetNotificationTemplateQuery ──────────────────────────────────────────────

public record GetNotificationTemplateQuery(Guid Id) : IRequest<NotificationTemplateDto>;

public sealed class GetNotificationTemplateQueryHandler
    : IRequestHandler<GetNotificationTemplateQuery, NotificationTemplateDto>
{
    private readonly IAppDbContext _db;

    public GetNotificationTemplateQueryHandler(IAppDbContext db) => _db = db;

    public async Task<NotificationTemplateDto> Handle(
        GetNotificationTemplateQuery req, CancellationToken ct)
    {
        var t = await _db.NotificationTemplates.AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"NotificationTemplate {req.Id} not found.");

        var catCode = t.CategoryId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.CategoryId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;
        var typeCode = t.TypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.TypeId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        return new NotificationTemplateDto(
            t.Id, t.CorporationId, t.Code,
            t.CategoryId, catCode, t.TypeId, typeCode,
            t.IsActive, t.CreatedAt, t.UpdatedAt, t.RowVersion,
            t.Translations.Select(tr => new NotificationTemplateTranslationDto(
                tr.Locale, tr.Subject, tr.Body)).ToList());
    }
}

// ── GetTriggerConfigsQuery ─────────────────────────────────────────────────────

public class GetTriggerConfigsQuery
    : PagedQuery, IRequest<PaginatedResult<NotificationTriggerConfigListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetTriggerConfigsQueryHandler
    : IRequestHandler<GetTriggerConfigsQuery, PaginatedResult<NotificationTriggerConfigListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetTriggerConfigsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<NotificationTriggerConfigListItemDto>> Handle(
        GetTriggerConfigsQuery req, CancellationToken ct)
    {
        var query =
            from cfg in _db.NotificationTriggerConfigs.AsNoTracking()
            join tmpl in _db.NotificationTemplates.AsNoTracking()
                on cfg.TemplateId equals tmpl.Id into tmplGrp
            from tmpl in tmplGrp.DefaultIfEmpty()
            select new NotificationTriggerConfigListItemDto(
                cfg.Id, cfg.CorporationId, cfg.TriggerCode,
                tmpl != null ? tmpl.Code : null,
                cfg.OffsetMinutes, cfg.IsActive,
                cfg.Channels.Count);

        if (req.CorporationId.HasValue)
            query = query.Where(x => x.CorporationId == req.CorporationId.Value || x.CorporationId == null);

        if (req.IsActive.HasValue)
            query = query.Where(x => x.IsActive == req.IsActive.Value);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "triggercode" => req.IsDescending
                ? query.OrderByDescending(x => x.TriggerCode)
                : query.OrderBy(x => x.TriggerCode),
            _ => query.OrderBy(x => x.TriggerCode)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<NotificationTriggerConfigListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}
