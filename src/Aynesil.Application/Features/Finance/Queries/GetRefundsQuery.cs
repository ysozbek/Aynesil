using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetRefundsQuery ───────────────────────────────────────────────────────────

public class GetRefundsQuery : PagedQuery, IRequest<PaginatedResult<RefundDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? PaymentId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed class GetRefundsQueryHandler
    : IRequestHandler<GetRefundsQuery, PaginatedResult<RefundDto>>
{
    private readonly IAppDbContext _db;

    public GetRefundsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<RefundDto>> Handle(
        GetRefundsQuery req, CancellationToken ct)
    {
        var q = _db.Refunds.AsNoTracking();

        if (req.CorporationId.HasValue) q = q.Where(r => r.CorporationId == req.CorporationId.Value);
        if (req.PaymentId.HasValue)     q = q.Where(r => r.PaymentId == req.PaymentId.Value);
        if (req.Status is not null)     q = q.Where(r => r.Status == req.Status);
        if (req.From.HasValue)          q = q.Where(r => r.CreatedAt >= req.From.Value);
        if (req.To.HasValue)            q = q.Where(r => r.CreatedAt <= req.To.Value);

        var query = q.Select(r => new RefundDto(
            r.Id, r.CorporationId, r.PaymentId,
            r.Amount, r.Reason, r.Status,
            r.ProcessedAt, r.CreatedAt));

        query = req.SortBy?.ToLower() switch
        {
            "amount"      => req.IsDescending ? query.OrderByDescending(r => r.Amount)    : query.OrderBy(r => r.Amount),
            "processedat" => req.IsDescending ? query.OrderByDescending(r => r.ProcessedAt) : query.OrderBy(r => r.ProcessedAt),
            _             => query.OrderByDescending(r => r.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        return PaginatedResult<RefundDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetRefundQuery ────────────────────────────────────────────────────────────

public record GetRefundQuery(Guid Id) : IRequest<RefundDto>;

public sealed class GetRefundQueryHandler : IRequestHandler<GetRefundQuery, RefundDto>
{
    private readonly IAppDbContext _db;

    public GetRefundQueryHandler(IAppDbContext db) => _db = db;

    public async Task<RefundDto> Handle(GetRefundQuery req, CancellationToken ct)
    {
        var r = await _db.Refunds.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Refund {req.Id} not found.");

        return new RefundDto(
            r.Id, r.CorporationId, r.PaymentId,
            r.Amount, r.Reason, r.Status,
            r.ProcessedAt, r.CreatedAt);
    }
}
