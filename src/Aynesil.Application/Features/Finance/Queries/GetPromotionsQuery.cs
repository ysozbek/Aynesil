using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetPromotionsQuery ────────────────────────────────────────────────────────

public class GetPromotionsQuery : PagedQuery, IRequest<PaginatedResult<PromotionListItemDto>>
{
    public bool? IsActive { get; set; }
    public bool? ValidToday { get; set; }
}

public sealed class GetPromotionsQueryHandler
    : IRequestHandler<GetPromotionsQuery, PaginatedResult<PromotionListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPromotionsQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResult<PromotionListItemDto>> Handle(
        GetPromotionsQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var q = _db.Promotions.AsNoTracking();

        q = q.Where(p => p.CorporationId == corporationId);
        if (req.IsActive.HasValue)      q = q.Where(p => p.IsActive == req.IsActive.Value);

        if (req.ValidToday == true)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            q = q.Where(p => p.IsActive
                && (p.ValidFrom == null || p.ValidFrom <= today)
                && (p.ValidTo   == null || p.ValidTo   >= today));
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(p => p.Code.ToLower().Contains(s) || p.Name.ToLower().Contains(s));
        }

        var sorted = req.SortBy?.ToLower() switch
        {
            "code"      => req.IsDescending ? q.OrderByDescending(p => p.Code)      : q.OrderBy(p => p.Code),
            "validfrom" => req.IsDescending ? q.OrderByDescending(p => p.ValidFrom) : q.OrderBy(p => p.ValidFrom),
            _           => q.OrderBy(p => p.Code)
        };

        var total = await sorted.CountAsync(ct);
        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(p => new PromotionListItemDto(
                p.Id, p.Code, p.Name, p.IsPercentage, p.Value,
                p.ValidFrom, p.ValidTo, p.IsActive))
            .ToListAsync(ct);

        return PaginatedResult<PromotionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetPromotionQuery ─────────────────────────────────────────────────────────

public record GetPromotionQuery(Guid Id) : IRequest<PromotionDto>;

public sealed class GetPromotionQueryHandler : IRequestHandler<GetPromotionQuery, PromotionDto>
{
    private readonly IAppDbContext _db;

    public GetPromotionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PromotionDto> Handle(GetPromotionQuery req, CancellationToken ct)
    {
        var p = await _db.Promotions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Promotion {req.Id} not found.");

        return new PromotionDto(
            p.Id, p.CorporationId, p.Code, p.Name,
            p.IsPercentage, p.Value,
            p.ValidFrom, p.ValidTo, p.MaxRedemptions, p.IsActive);
    }
}

// ── ValidatePromotionCodeQuery ────────────────────────────────────────────────

public record ValidatePromotionCodeQuery( string Code)
    : IRequest<PromotionDto?>;

public sealed class ValidatePromotionCodeQueryHandler
    : IRequestHandler<ValidatePromotionCodeQuery, PromotionDto?>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ValidatePromotionCodeQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PromotionDto?> Handle(ValidatePromotionCodeQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var p = await _db.Promotions.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.CorporationId == corporationId &&
                x.Code == req.Code &&
                x.IsActive &&
                (x.ValidFrom == null || x.ValidFrom <= today) &&
                (x.ValidTo   == null || x.ValidTo   >= today), ct);

        return p is null ? null : new PromotionDto(
            p.Id, p.CorporationId, p.Code, p.Name,
            p.IsPercentage, p.Value,
            p.ValidFrom, p.ValidTo, p.MaxRedemptions, p.IsActive);
    }
}
