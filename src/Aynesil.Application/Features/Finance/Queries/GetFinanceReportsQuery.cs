using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetRevenueReportQuery ─────────────────────────────────────────────────────

public record GetRevenueReportQuery(
    Guid CorporationId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<RevenueReportDto>;

public sealed class GetRevenueReportQueryHandler
    : IRequestHandler<GetRevenueReportQuery, RevenueReportDto>
{
    private readonly IAppDbContext _db;

    public GetRevenueReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<RevenueReportDto> Handle(GetRevenueReportQuery req, CancellationToken ct)
    {
        var fromUtc = req.PeriodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc   = req.PeriodEnd.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var payments = await _db.Payments.AsNoTracking()
            .Where(p =>
                p.CorporationId == req.CorporationId &&
                p.Status == "captured" &&
                p.PaidAt >= fromUtc && p.PaidAt <= toUtc)
            .Select(p => new { p.Amount, p.PaymentMethodId })
            .ToListAsync(ct);

        var refunds = await _db.Refunds.AsNoTracking()
            .Where(r =>
                r.CorporationId == req.CorporationId &&
                r.Status == "processed" &&
                r.ProcessedAt >= fromUtc && r.ProcessedAt <= toUtc)
            .Select(r => r.Amount)
            .ToListAsync(ct);

        var byMethod = payments
            .GroupBy(p => p.PaymentMethodId)
            .Select(g => new RevenueByMethodDto(
                PaymentMethodId: g.Key,
                MethodName:      g.Key.HasValue ? g.Key.ToString()! : "Unknown",
                TotalAmount:     g.Sum(p => p.Amount),
                Count:           g.Count()))
            .ToList();

        return new RevenueReportDto(
            PeriodStart:   req.PeriodStart,
            PeriodEnd:     req.PeriodEnd,
            TotalRevenue:  payments.Sum(p => p.Amount),
            TotalRefunded: refunds.Sum(),
            NetRevenue:    payments.Sum(p => p.Amount) - refunds.Sum(),
            PaymentCount:  payments.Count,
            RefundCount:   refunds.Count,
            ByMethod:      byMethod);
    }
}

// ── GetPackageReportQuery ─────────────────────────────────────────────────────

public record GetPackageReportQuery(Guid CorporationId) : IRequest<PackageReportDto>;

public sealed class GetPackageReportQueryHandler
    : IRequestHandler<GetPackageReportQuery, PackageReportDto>
{
    private readonly IAppDbContext _db;

    public GetPackageReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PackageReportDto> Handle(GetPackageReportQuery req, CancellationToken ct)
    {
        var packages = await _db.StudentPackages.AsNoTracking()
            .Where(p => p.CorporationId == req.CorporationId)
            .Select(p => new
            {
                p.Id,
                p.Status,
                p.ExpiresOn,
                p.TotalCredits,
                p.PackageDefinitionId,
                p.Price
            })
            .ToListAsync(ct);

        var threshold30 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var allPackageIds = packages.Select(p => p.Id).ToList();

        var ledgerTotals = await _db.CreditLedgerEntries.AsNoTracking()
            .Where(e => allPackageIds.Contains(e.StudentPackageId))
            .GroupBy(e => e.EntryType)
            .Select(g => new { Type = g.Key, Total = g.Sum(e => e.Delta) })
            .ToListAsync(ct);

        var totalGranted    = ledgerTotals.Where(e => e.Type == "grant").Sum(e => e.Total);
        var totalConsumed   = Math.Abs(ledgerTotals.Where(e => e.Type == "consume").Sum(e => e.Total));
        var totalRemaining  = ledgerTotals.Sum(e => e.Total);

        var topPackages = packages
            .Where(p => p.PackageDefinitionId.HasValue)
            .GroupBy(p => p.PackageDefinitionId)
            .Select(g => new PackageSalesSummaryDto(
                PackageDefinitionId: g.Key,
                PackageName:         g.Key.HasValue ? g.Key.ToString()! : "Custom",
                UnitsSold:           g.Count(),
                TotalRevenue:        g.Sum(p => p.Price)))
            .OrderByDescending(s => s.UnitsSold)
            .Take(10)
            .ToList();

        return new PackageReportDto(
            CorporationId:       req.CorporationId,
            TotalActive:         packages.Count(p => p.Status == "active"),
            TotalExhausted:      packages.Count(p => p.Status == "exhausted"),
            TotalExpired:        packages.Count(p => p.Status == "expired"),
            TotalCancelled:      packages.Count(p => p.Status == "cancelled"),
            ExpiringIn30Days:    packages.Count(p => p.Status == "active" && p.ExpiresOn <= threshold30),
            TotalCreditsGranted:   totalGranted,
            TotalCreditsConsumed:  totalConsumed,
            TotalCreditsRemaining: totalRemaining,
            TopPackages:           topPackages);
    }
}

// ── GetCreditUsageReportQuery ─────────────────────────────────────────────────

public record GetCreditUsageReportQuery(
    Guid CorporationId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<CreditUsageReportDto>;

public sealed class GetCreditUsageReportQueryHandler
    : IRequestHandler<GetCreditUsageReportQuery, CreditUsageReportDto>
{
    private readonly IAppDbContext _db;

    public GetCreditUsageReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CreditUsageReportDto> Handle(
        GetCreditUsageReportQuery req, CancellationToken ct)
    {
        var fromUtc = req.PeriodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc   = req.PeriodEnd.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var entries = await _db.CreditLedgerEntries.AsNoTracking()
            .Where(e =>
                e.CorporationId == req.CorporationId &&
                e.OccurredAt >= fromUtc && e.OccurredAt <= toUtc)
            .ToListAsync(ct);

        // Per-student rollup: join through StudentPackage
        var packageIds = entries.Select(e => e.StudentPackageId).Distinct().ToList();

        var packageStudentMap = await _db.StudentPackages.AsNoTracking()
            .Where(p => packageIds.Contains(p.Id))
            .Select(p => new { p.Id, p.StudentId })
            .ToDictionaryAsync(p => p.Id, p => p.StudentId, ct);

        var studentIds = packageStudentMap.Values.Distinct().ToList();

        var studentNames = await _db.Students.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => new { s.Id, s.FirstName, s.LastName })
            .ToDictionaryAsync(s => s.Id, s => $"{s.FirstName} {s.LastName}".Trim(), ct);

        var byStudent = entries
            .GroupBy(e => packageStudentMap.GetValueOrDefault(e.StudentPackageId))
            .Where(g => g.Key != default)
            .Select(g =>
            {
                var sid = g.Key;
                return new CreditUsageByStudentDto(
                    StudentId:       sid,
                    StudentFullName: studentNames.GetValueOrDefault(sid, ""),
                    Granted:         g.Where(e => e.EntryType == "grant").Sum(e => e.Delta),
                    Consumed:        Math.Abs(g.Where(e => e.EntryType == "consume").Sum(e => e.Delta)),
                    Remaining:       g.Sum(e => e.Delta));
            })
            .OrderByDescending(s => s.Consumed)
            .ToList();

        return new CreditUsageReportDto(
            PeriodStart:    req.PeriodStart,
            PeriodEnd:      req.PeriodEnd,
            TotalGranted:   entries.Where(e => e.EntryType == "grant").Sum(e => e.Delta),
            TotalConsumed:  Math.Abs(entries.Where(e => e.EntryType == "consume").Sum(e => e.Delta)),
            TotalExpired:   Math.Abs(entries.Where(e => e.EntryType == "expire").Sum(e => e.Delta)),
            TotalRefunded:  entries.Where(e => e.EntryType == "refund").Sum(e => e.Delta),
            TotalAdjusted:  entries.Where(e => e.EntryType == "adjustment").Sum(e => e.Delta),
            ByStudent:      byStudent);
    }
}
