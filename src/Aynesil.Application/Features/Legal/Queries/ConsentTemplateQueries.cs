using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Queries;

// ── GetConsentTemplatesQuery ──────────────────────────────────────────────────

public record GetConsentTemplatesQuery(
    Guid CorporationId,
    Guid? ConsentTypeId = null,
    bool? IsCurrent = null,
    bool? IsMandatory = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<ConsentTemplateListItemDto>>;

public sealed class GetConsentTemplatesQueryHandler
    : IRequestHandler<GetConsentTemplatesQuery, PaginatedResult<ConsentTemplateListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetConsentTemplatesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ConsentTemplateListItemDto>> Handle(
        GetConsentTemplatesQuery req, CancellationToken ct)
    {
        var query = _db.ConsentTemplates
            .AsNoTracking()
            .Where(t => t.CorporationId == req.CorporationId);

        if (req.ConsentTypeId.HasValue)
            query = query.Where(t => t.ConsentTypeId == req.ConsentTypeId);

        if (req.IsCurrent.HasValue)
            query = query.Where(t => t.IsCurrent == req.IsCurrent.Value);

        if (req.IsMandatory.HasValue)
            query = query.Where(t => t.IsMandatory == req.IsMandatory.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(t => new
            {
                t.Id, t.CorporationId, t.Code, t.ConsentTypeId,
                t.Version, t.IsCurrent, t.IsMandatory, t.EffectiveFrom, t.CreatedAt, t.UpdatedAt
            })
            .ToListAsync(ct);

        var typeIds = items.Where(t => t.ConsentTypeId.HasValue)
            .Select(t => t.ConsentTypeId!.Value).Distinct().ToList();

        var typeCodes = typeIds.Count > 0
            ? await _db.RefValues.AsNoTracking()
                .Where(r => typeIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Code })
                .ToDictionaryAsync(r => r.Id, r => r.Code, ct)
            : new Dictionary<Guid, string>();

        var results = items.Select(t => new ConsentTemplateListItemDto(
            t.Id, t.CorporationId, t.Code,
            t.ConsentTypeId, t.ConsentTypeId.HasValue ? typeCodes.GetValueOrDefault(t.ConsentTypeId.Value) : null,
            t.Version, t.IsCurrent, t.IsMandatory, t.EffectiveFrom, t.CreatedAt, t.UpdatedAt)).ToList();

        return PaginatedResult<ConsentTemplateListItemDto>.Create(results, total, req.Page, req.PageSize);
    }
}

// ── GetConsentTemplateQuery ───────────────────────────────────────────────────

public record GetConsentTemplateQuery(Guid Id) : IRequest<ConsentTemplateDto>;

public sealed class GetConsentTemplateQueryHandler
    : IRequestHandler<GetConsentTemplateQuery, ConsentTemplateDto>
{
    private readonly IAppDbContext _db;

    public GetConsentTemplateQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ConsentTemplateDto> Handle(GetConsentTemplateQuery req, CancellationToken ct)
    {
        var t = await _db.ConsentTemplates
            .Include(x => x.Translations)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consent template {req.Id} not found.");

        string? typeCode = null;
        if (t.ConsentTypeId.HasValue)
            typeCode = await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.ConsentTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct);

        return new ConsentTemplateDto(
            t.Id, t.CorporationId, t.Code,
            t.ConsentTypeId, typeCode,
            t.Version, t.IsCurrent, t.IsMandatory, t.EffectiveFrom,
            t.Translations.Select(x => new ConsentTemplateTranslationDto(x.Locale, x.Title, x.Body)).ToList(),
            t.CreatedAt, t.UpdatedAt, t.RowVersion);
    }
}

// ── GetConsentTemplateVersionsQuery ──────────────────────────────────────────

public record GetConsentTemplateVersionsQuery(
    Guid CorporationId,
    string Code) : IRequest<IReadOnlyList<ConsentTemplateListItemDto>>;

public sealed class GetConsentTemplateVersionsQueryHandler
    : IRequestHandler<GetConsentTemplateVersionsQuery, IReadOnlyList<ConsentTemplateListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetConsentTemplateVersionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConsentTemplateListItemDto>> Handle(
        GetConsentTemplateVersionsQuery req, CancellationToken ct)
    {
        var items = await _db.ConsentTemplates
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.CorporationId == req.CorporationId
                     && t.Code == req.Code.ToLowerInvariant())
            .OrderByDescending(t => t.Version)
            .Select(t => new
            {
                t.Id, t.CorporationId, t.Code, t.ConsentTypeId,
                t.Version, t.IsCurrent, t.IsMandatory, t.EffectiveFrom, t.CreatedAt, t.UpdatedAt
            })
            .ToListAsync(ct);

        var typeIds = items.Where(t => t.ConsentTypeId.HasValue)
            .Select(t => t.ConsentTypeId!.Value).Distinct().ToList();

        var typeCodes = typeIds.Count > 0
            ? await _db.RefValues.AsNoTracking()
                .Where(r => typeIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Code })
                .ToDictionaryAsync(r => r.Id, r => r.Code, ct)
            : new Dictionary<Guid, string>();

        return items.Select(t => new ConsentTemplateListItemDto(
            t.Id, t.CorporationId, t.Code,
            t.ConsentTypeId, t.ConsentTypeId.HasValue ? typeCodes.GetValueOrDefault(t.ConsentTypeId.Value) : null,
            t.Version, t.IsCurrent, t.IsMandatory, t.EffectiveFrom, t.CreatedAt, t.UpdatedAt)).ToList();
    }
}
