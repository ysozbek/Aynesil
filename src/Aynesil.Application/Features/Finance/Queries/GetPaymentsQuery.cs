using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetPaymentsQuery ──────────────────────────────────────────────────────────

public class GetPaymentsQuery : PagedQuery, IRequest<PaginatedResult<PaymentListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? PaidFrom { get; set; }
    public DateTimeOffset? PaidTo { get; set; }
}

public sealed class GetPaymentsQueryHandler
    : IRequestHandler<GetPaymentsQuery, PaginatedResult<PaymentListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetPaymentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<PaymentListItemDto>> Handle(
        GetPaymentsQuery req, CancellationToken ct)
    {
        var q = _db.Payments.AsNoTracking();

        if (req.CorporationId.HasValue)  q = q.Where(p => p.CorporationId == req.CorporationId.Value);
        if (req.InvoiceId.HasValue)      q = q.Where(p => p.InvoiceId == req.InvoiceId.Value);
        if (req.StudentId.HasValue)      q = q.Where(p => p.StudentId == req.StudentId.Value);
        if (req.PaymentMethodId.HasValue) q = q.Where(p => p.PaymentMethodId == req.PaymentMethodId.Value);
        if (req.Status is not null)      q = q.Where(p => p.Status == req.Status);
        if (req.PaidFrom.HasValue)       q = q.Where(p => p.PaidAt >= req.PaidFrom.Value);
        if (req.PaidTo.HasValue)         q = q.Where(p => p.PaidAt <= req.PaidTo.Value);

        var joined =
            from pay in q
            join student in _db.Students.AsNoTracking()
                on pay.StudentId equals student.Id into studentGrp
            from student in studentGrp.DefaultIfEmpty()
            select new { pay, student };

        var sorted = req.SortBy?.ToLower() switch
        {
            "amount"    => req.IsDescending ? joined.OrderByDescending(x => x.pay.Amount)    : joined.OrderBy(x => x.pay.Amount),
            "paidat"    => req.IsDescending ? joined.OrderByDescending(x => x.pay.PaidAt)    : joined.OrderBy(x => x.pay.PaidAt),
            "createdat" => req.IsDescending ? joined.OrderByDescending(x => x.pay.CreatedAt) : joined.OrderBy(x => x.pay.CreatedAt),
            _           => joined.OrderByDescending(x => x.pay.CreatedAt)
        };

        var total = await sorted.CountAsync(ct);
        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(x => new PaymentListItemDto(
                x.pay.Id, x.pay.InvoiceId,
                x.student != null ? x.student.FirstName + " " + x.student.LastName : null,
                x.pay.PaymentMethodId, x.pay.Amount, x.pay.Currency,
                x.pay.Status, x.pay.PaidAt, x.pay.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<PaymentListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetPaymentQuery ───────────────────────────────────────────────────────────

public record GetPaymentQuery(Guid Id) : IRequest<PaymentDto>;

public sealed class GetPaymentQueryHandler : IRequestHandler<GetPaymentQuery, PaymentDto>
{
    private readonly IAppDbContext _db;

    public GetPaymentQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaymentDto> Handle(GetPaymentQuery req, CancellationToken ct)
    {
        var p = await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Payment {req.Id} not found.");

        string? studentName = null;
        if (p.StudentId.HasValue)
        {
            var s = await _db.Students.AsNoTracking()
                .Where(st => st.Id == p.StudentId.Value)
                .Select(st => new { st.FirstName, st.LastName })
                .FirstOrDefaultAsync(ct);
            studentName = s is null ? null : $"{s.FirstName} {s.LastName}".Trim();
        }

        return FinanceProjection.ToPaymentDto(p, studentName);
    }
}
