using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Queries;

// ── GetContractTemplatesQuery ─────────────────────────────────────────────────

public record GetContractTemplatesQuery(
    Guid CorporationId,
    Guid? ContractTypeId = null,
    bool? IsCurrent = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<ContractTemplateListItemDto>>;

public sealed class GetContractTemplatesQueryHandler
    : IRequestHandler<GetContractTemplatesQuery, PaginatedResult<ContractTemplateListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetContractTemplatesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ContractTemplateListItemDto>> Handle(
        GetContractTemplatesQuery req, CancellationToken ct)
    {
        var query = _db.ContractTemplates
            .AsNoTracking()
            .Where(t => t.CorporationId == req.CorporationId);

        if (req.ContractTypeId.HasValue)
            query = query.Where(t => t.ContractTypeId == req.ContractTypeId);

        if (req.IsCurrent.HasValue)
            query = query.Where(t => t.IsCurrent == req.IsCurrent.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(t => new
            {
                t.Id, t.CorporationId, t.Code, t.ContractTypeId,
                t.Version, t.IsCurrent, t.EffectiveFrom, t.CreatedAt, t.UpdatedAt
            })
            .ToListAsync(ct);

        var typeIds = items.Where(t => t.ContractTypeId.HasValue)
            .Select(t => t.ContractTypeId!.Value).Distinct().ToList();

        var typeCodes = typeIds.Count > 0
            ? await _db.RefValues.AsNoTracking()
                .Where(r => typeIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Code })
                .ToDictionaryAsync(r => r.Id, r => r.Code, ct)
            : new Dictionary<Guid, string>();

        var results = items.Select(t => new ContractTemplateListItemDto(
            t.Id, t.CorporationId, t.Code,
            t.ContractTypeId, t.ContractTypeId.HasValue ? typeCodes.GetValueOrDefault(t.ContractTypeId.Value) : null,
            t.Version, t.IsCurrent, t.EffectiveFrom, t.CreatedAt, t.UpdatedAt)).ToList();

        return PaginatedResult<ContractTemplateListItemDto>.Create(results, total, req.Page, req.PageSize);
    }
}

// ── GetContractTemplateQuery ──────────────────────────────────────────────────

public record GetContractTemplateQuery(Guid Id) : IRequest<ContractTemplateDto>;

public sealed class GetContractTemplateQueryHandler
    : IRequestHandler<GetContractTemplateQuery, ContractTemplateDto>
{
    private readonly IAppDbContext _db;

    public GetContractTemplateQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ContractTemplateDto> Handle(GetContractTemplateQuery req, CancellationToken ct)
    {
        var t = await _db.ContractTemplates
            .Include(x => x.Translations)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Contract template {req.Id} not found.");

        string? typeCode = null;
        if (t.ContractTypeId.HasValue)
            typeCode = await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.ContractTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct);

        return new ContractTemplateDto(
            t.Id, t.CorporationId, t.Code,
            t.ContractTypeId, typeCode,
            t.Version, t.IsCurrent, t.EffectiveFrom,
            t.Translations.Select(x => new ContractTemplateTranslationDto(x.Locale, x.Title, x.Body)).ToList(),
            t.CreatedAt, t.UpdatedAt, t.RowVersion);
    }
}

// ── GetContractTemplateVersionsQuery ─────────────────────────────────────────

/// <summary>Returns all versions (including archived) for a given template code.</summary>
public record GetContractTemplateVersionsQuery(
    Guid CorporationId,
    string Code) : IRequest<IReadOnlyList<ContractTemplateListItemDto>>;

public sealed class GetContractTemplateVersionsQueryHandler
    : IRequestHandler<GetContractTemplateVersionsQuery, IReadOnlyList<ContractTemplateListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetContractTemplateVersionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContractTemplateListItemDto>> Handle(
        GetContractTemplateVersionsQuery req, CancellationToken ct)
    {
        // IgnoreQueryFilters to include soft-deleted archived versions.
        var items = await _db.ContractTemplates
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.CorporationId == req.CorporationId
                     && t.Code == req.Code.ToLowerInvariant())
            .OrderByDescending(t => t.Version)
            .Select(t => new
            {
                t.Id, t.CorporationId, t.Code, t.ContractTypeId,
                t.Version, t.IsCurrent, t.EffectiveFrom, t.CreatedAt, t.UpdatedAt
            })
            .ToListAsync(ct);

        var typeIds = items.Where(t => t.ContractTypeId.HasValue)
            .Select(t => t.ContractTypeId!.Value).Distinct().ToList();

        var typeCodes = typeIds.Count > 0
            ? await _db.RefValues.AsNoTracking()
                .Where(r => typeIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Code })
                .ToDictionaryAsync(r => r.Id, r => r.Code, ct)
            : new Dictionary<Guid, string>();

        return items.Select(t => new ContractTemplateListItemDto(
            t.Id, t.CorporationId, t.Code,
            t.ContractTypeId, t.ContractTypeId.HasValue ? typeCodes.GetValueOrDefault(t.ContractTypeId.Value) : null,
            t.Version, t.IsCurrent, t.EffectiveFrom, t.CreatedAt, t.UpdatedAt)).ToList();
    }
}
