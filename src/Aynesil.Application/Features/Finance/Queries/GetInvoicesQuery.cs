using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetInvoicesQuery ──────────────────────────────────────────────────────────

public class GetInvoicesQuery : PagedQuery, IRequest<PaginatedResult<InvoiceListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? StudentId { get; set; }
    public string? Status { get; set; }
    public DateOnly? IssuedFrom { get; set; }
    public DateOnly? IssuedTo { get; set; }
    public DateOnly? DueFrom { get; set; }
    public DateOnly? DueTo { get; set; }
    public bool? Overdue { get; set; }
}

public sealed class GetInvoicesQueryHandler
    : IRequestHandler<GetInvoicesQuery, PaginatedResult<InvoiceListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetInvoicesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<InvoiceListItemDto>> Handle(
        GetInvoicesQuery req, CancellationToken ct)
    {
        var q = _db.Invoices.AsNoTracking();

        if (req.CorporationId.HasValue) q = q.Where(i => i.CorporationId == req.CorporationId.Value);
        if (req.StudentId.HasValue)     q = q.Where(i => i.StudentId == req.StudentId.Value);
        if (req.Status is not null)     q = q.Where(i => i.Status == req.Status);
        if (req.IssuedFrom.HasValue)    q = q.Where(i => i.IssueDate >= req.IssuedFrom.Value);
        if (req.IssuedTo.HasValue)      q = q.Where(i => i.IssueDate <= req.IssuedTo.Value);
        if (req.DueFrom.HasValue)       q = q.Where(i => i.DueDate >= req.DueFrom.Value);
        if (req.DueTo.HasValue)         q = q.Where(i => i.DueDate <= req.DueTo.Value);

        if (req.Overdue == true)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            q = q.Where(i => (i.Status == "issued" || i.Status == "partial") && i.DueDate < today);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(i => i.InvoiceNo != null && i.InvoiceNo.ToLower().Contains(s));
        }

        var query =
            from inv in q
            join student in _db.Students.AsNoTracking()
                on inv.StudentId equals student.Id into studentGrp
            from student in studentGrp.DefaultIfEmpty()
            select new
            {
                inv,
                StudentFullName = student != null ? student.FirstName + " " + student.LastName : null,
                PaidAmount      = _db.Payments.AsNoTracking()
                    .Where(p => p.InvoiceId == inv.Id && p.Status == "captured")
                    .Sum(p => (decimal?)p.Amount) ?? 0m
            };

        var sorted = req.SortBy?.ToLower() switch
        {
            "issuedate"  => req.IsDescending ? query.OrderByDescending(x => x.inv.IssueDate)  : query.OrderBy(x => x.inv.IssueDate),
            "duedate"    => req.IsDescending ? query.OrderByDescending(x => x.inv.DueDate)    : query.OrderBy(x => x.inv.DueDate),
            "grandtotal" => req.IsDescending ? query.OrderByDescending(x => x.inv.GrandTotal) : query.OrderBy(x => x.inv.GrandTotal),
            _            => query.OrderByDescending(x => x.inv.IssueDate)
        };

        var total = await sorted.CountAsync(ct);
        var page  = await sorted.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        var items = page.Select(x => new InvoiceListItemDto(
            x.inv.Id, x.inv.CorporationId,
            x.inv.StudentId, x.StudentFullName?.Trim(),
            x.inv.InvoiceNo, x.inv.IssueDate, x.inv.DueDate,
            x.inv.Currency, x.inv.GrandTotal, x.PaidAmount,
            x.inv.Status)).ToList();

        return PaginatedResult<InvoiceListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetInvoiceQuery ───────────────────────────────────────────────────────────

public record GetInvoiceQuery(Guid Id) : IRequest<InvoiceDto>;

public sealed class GetInvoiceQueryHandler : IRequestHandler<GetInvoiceQuery, InvoiceDto>
{
    private readonly IAppDbContext _db;

    public GetInvoiceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(GetInvoiceQuery req, CancellationToken ct)
        => await FinanceProjection.LoadInvoiceAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Invoice {req.Id} not found.");
}
