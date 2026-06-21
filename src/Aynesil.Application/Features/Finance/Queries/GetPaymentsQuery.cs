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

        var query =
            from pay in q
            join student in _db.Students.AsNoTracking()
                on pay.StudentId equals student.Id into studentGrp
            from student in studentGrp.DefaultIfEmpty()
            select new PaymentListItemDto(
                pay.Id, pay.InvoiceId,
                student != null ? student.FirstName + " " + student.LastName : null,
                pay.PaymentMethodId, pay.Amount, pay.Currency,
                pay.Status, pay.PaidAt, pay.CreatedAt);

        query = req.SortBy?.ToLower() switch
        {
            "amount"    => req.IsDescending ? query.OrderByDescending(p => p.Amount)    : query.OrderBy(p => p.Amount),
            "paidat"    => req.IsDescending ? query.OrderByDescending(p => p.PaidAt)    : query.OrderBy(p => p.PaidAt),
            "createdat" => req.IsDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _           => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

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
