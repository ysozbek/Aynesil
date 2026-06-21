using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetPromotionsQuery ────────────────────────────────────────────────────────

public class GetPromotionsQuery : PagedQuery, IRequest<PaginatedResult<PromotionListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public bool? IsActive { get; set; }
    public bool? ValidToday { get; set; }
}

public sealed class GetPromotionsQueryHandler
    : IRequestHandler<GetPromotionsQuery, PaginatedResult<PromotionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetPromotionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<PromotionListItemDto>> Handle(
        GetPromotionsQuery req, CancellationToken ct)
    {
        var q = _db.Promotions.AsNoTracking();

        if (req.CorporationId.HasValue) q = q.Where(p => p.CorporationId == req.CorporationId.Value);
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

        var query = q.Select(p => new PromotionListItemDto(
            p.Id, p.Code, p.Name, p.IsPercentage, p.Value,
            p.ValidFrom, p.ValidTo, p.IsActive));

        query = req.SortBy?.ToLower() switch
        {
            "code"      => req.IsDescending ? query.OrderByDescending(p => p.Code)  : query.OrderBy(p => p.Code),
            "validfrom" => req.IsDescending ? query.OrderByDescending(p => p.ValidFrom) : query.OrderBy(p => p.ValidFrom),
            _           => query.OrderBy(p => p.Code)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

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

public record ValidatePromotionCodeQuery(Guid CorporationId, string Code)
    : IRequest<PromotionDto?>;

public sealed class ValidatePromotionCodeQueryHandler
    : IRequestHandler<ValidatePromotionCodeQuery, PromotionDto?>
{
    private readonly IAppDbContext _db;

    public ValidatePromotionCodeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PromotionDto?> Handle(ValidatePromotionCodeQuery req, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var p = await _db.Promotions.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.CorporationId == req.CorporationId &&
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
