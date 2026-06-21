using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetCreditLedgerQuery ──────────────────────────────────────────────────────

public class GetCreditLedgerQuery : PagedQuery, IRequest<PaginatedResult<CreditLedgerEntryDto>>
{
    public Guid? StudentPackageId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? CorporationId { get; set; }
    public string? EntryType { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed class GetCreditLedgerQueryHandler
    : IRequestHandler<GetCreditLedgerQuery, PaginatedResult<CreditLedgerEntryDto>>
{
    private readonly IAppDbContext _db;

    public GetCreditLedgerQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<CreditLedgerEntryDto>> Handle(
        GetCreditLedgerQuery req, CancellationToken ct)
    {
        var q = _db.CreditLedgerEntries.AsNoTracking();

        if (req.StudentPackageId.HasValue) q = q.Where(e => e.StudentPackageId == req.StudentPackageId.Value);
        if (req.CorporationId.HasValue)    q = q.Where(e => e.CorporationId == req.CorporationId.Value);
        if (req.EntryType is not null)     q = q.Where(e => e.EntryType == req.EntryType);
        if (req.From.HasValue)             q = q.Where(e => e.OccurredAt >= req.From.Value);
        if (req.To.HasValue)               q = q.Where(e => e.OccurredAt <= req.To.Value);

        if (req.StudentId.HasValue)
        {
            var packageIds = await _db.StudentPackages.AsNoTracking()
                .Where(p => p.StudentId == req.StudentId.Value)
                .Select(p => p.Id)
                .ToListAsync(ct);

            q = q.Where(e => packageIds.Contains(e.StudentPackageId));
        }

        // Order chronologically; running balance is computed client-side per package
        var ordered = q.OrderBy(e => e.OccurredAt);

        var total   = await ordered.CountAsync(ct);
        var entries = await ordered.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        // Compute running balance per package within the page
        var balanceByPackage = new Dictionary<Guid, decimal>();
        var items = entries.Select(e =>
        {
            balanceByPackage.TryGetValue(e.StudentPackageId, out var prev);
            var running = prev + e.Delta;
            balanceByPackage[e.StudentPackageId] = running;

            return new CreditLedgerEntryDto(
                e.Id, e.StudentPackageId, e.EntryType,
                e.Delta, running,
                e.SessionId, e.Reason, e.OccurredAt, e.CreatedBy);
        }).ToList();

        return PaginatedResult<CreditLedgerEntryDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetCreditSummaryQuery ─────────────────────────────────────────────────────

public record GetCreditSummaryQuery(Guid StudentId, Guid? CorporationId = null)
    : IRequest<CreditSummaryDto>;

public sealed class GetCreditSummaryQueryHandler
    : IRequestHandler<GetCreditSummaryQuery, CreditSummaryDto>
{
    private readonly IAppDbContext _db;

    public GetCreditSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CreditSummaryDto> Handle(GetCreditSummaryQuery req, CancellationToken ct)
    {
        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == req.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var packages = await _db.StudentPackages.AsNoTracking()
            .Where(p => p.StudentId == req.StudentId)
            .ToListAsync(ct);

        var activePackages = packages.Where(p => p.Status == "active").ToList();
        var allPackageIds  = packages.Select(p => p.Id).ToList();

        var ledger = await _db.CreditLedgerEntries.AsNoTracking()
            .Where(e => allPackageIds.Contains(e.StudentPackageId))
            .ToListAsync(ct);

        var threshold30 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        return new CreditSummaryDto(
            StudentId:        req.StudentId,
            StudentFullName:  $"{student.FirstName} {student.LastName}".Trim(),
            ActivePackages:   activePackages.Count,
            TotalGranted:     ledger.Where(e => e.EntryType is "grant").Sum(e => e.Delta),
            TotalConsumed:    ledger.Where(e => e.EntryType is "consume").Sum(e => Math.Abs(e.Delta)),
            TotalRemaining:   ledger.Sum(e => e.Delta),
            ExpiringWithin30Days: activePackages.Count(p =>
                p.ExpiresOn.HasValue && p.ExpiresOn.Value <= threshold30));
    }
}
