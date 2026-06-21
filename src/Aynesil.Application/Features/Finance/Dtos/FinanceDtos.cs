using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Dtos;

// ── Package Definition DTOs ───────────────────────────────────────────────────

public record PackageDefinitionDto(
    Guid Id,
    Guid CorporationId,
    string Code,
    string Name,
    Guid? PackageTypeId,
    Guid? ProgramId,
    decimal? TotalCredits,
    int? ValidityDays,
    decimal ListPrice,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

public record PackageDefinitionListItemDto(
    Guid Id,
    Guid CorporationId,
    string Code,
    string Name,
    Guid? PackageTypeId,
    decimal? TotalCredits,
    decimal ListPrice,
    string Currency,
    bool IsActive);

// ── Student Package DTOs ──────────────────────────────────────────────────────

public record StudentPackageDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    string StudentFullName,
    Guid? PackageDefinitionId,
    string? PackageDefinitionName,
    DateOnly PurchasedOn,
    DateOnly? ExpiresOn,
    decimal TotalCredits,
    decimal RemainingCredits,
    decimal Price,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

public record StudentPackageListItemDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    Guid? PackageDefinitionId,
    string? PackageDefinitionName,
    DateOnly PurchasedOn,
    DateOnly? ExpiresOn,
    decimal TotalCredits,
    decimal RemainingCredits,
    string Status);

public record PackageBalanceDto(
    Guid StudentPackageId,
    Guid StudentId,
    decimal TotalCredits,
    decimal RemainingCredits,
    decimal ConsumedCredits,
    DateOnly? ExpiresOn,
    string Status);

// ── Credit Ledger DTOs ────────────────────────────────────────────────────────

public record CreditLedgerEntryDto(
    Guid Id,
    Guid StudentPackageId,
    string EntryType,
    decimal Delta,
    decimal RunningBalance,
    Guid? SessionId,
    string? Reason,
    DateTimeOffset OccurredAt,
    Guid? CreatedBy);

public record CreditSummaryDto(
    Guid StudentId,
    string StudentFullName,
    int ActivePackages,
    decimal TotalGranted,
    decimal TotalConsumed,
    decimal TotalRemaining,
    int ExpiringWithin30Days);

// ── Invoice DTOs ──────────────────────────────────────────────────────────────

public record InvoiceDto(
    Guid Id,
    Guid CorporationId,
    Guid? StudentId,
    string? StudentFullName,
    Guid? GuardianId,
    string? InvoiceNo,
    DateOnly IssueDate,
    DateOnly? DueDate,
    string Currency,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<DiscountDto> Discounts);

public record InvoiceListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? StudentId,
    string? StudentFullName,
    string? InvoiceNo,
    DateOnly IssueDate,
    DateOnly? DueDate,
    string Currency,
    decimal GrandTotal,
    decimal PaidAmount,
    string Status);

public record InvoiceLineDto(
    Guid Id,
    Guid InvoiceId,
    string Description,
    Guid? StudentPackageId,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int SortOrder);

// ── Payment DTOs ──────────────────────────────────────────────────────────────

public record PaymentDto(
    Guid Id,
    Guid CorporationId,
    Guid? InvoiceId,
    Guid? StudentId,
    string? StudentFullName,
    Guid? PaymentMethodId,
    decimal Amount,
    string Currency,
    string Status,
    Guid? GatewayProviderId,
    string? GatewayReference,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    int RowVersion);

public record PaymentListItemDto(
    Guid Id,
    Guid? InvoiceId,
    string? StudentFullName,
    Guid? PaymentMethodId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt);

// ── Refund DTOs ───────────────────────────────────────────────────────────────

public record RefundDto(
    Guid Id,
    Guid CorporationId,
    Guid PaymentId,
    decimal Amount,
    string? Reason,
    string Status,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset CreatedAt);

// ── Discount DTOs ─────────────────────────────────────────────────────────────

public record DiscountDto(
    Guid Id,
    Guid CorporationId,
    Guid? InvoiceId,
    Guid? StudentPackageId,
    Guid? DiscountTypeId,
    bool IsPercentage,
    decimal Value,
    string? Reason,
    DateTimeOffset CreatedAt);

// ── Scholarship DTOs ──────────────────────────────────────────────────────────

public record ScholarshipDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    string StudentFullName,
    Guid? ScholarshipTypeId,
    decimal? Percentage,
    decimal? Amount,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    Guid? ApprovedBy,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

public record ScholarshipListItemDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    Guid? ScholarshipTypeId,
    decimal? Percentage,
    decimal? Amount,
    DateOnly? ValidFrom,
    DateOnly? ValidTo);

// ── Promotion DTOs ────────────────────────────────────────────────────────────

public record PromotionDto(
    Guid Id,
    Guid CorporationId,
    string Code,
    string Name,
    bool IsPercentage,
    decimal Value,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? MaxRedemptions,
    bool IsActive);

public record PromotionListItemDto(
    Guid Id,
    string Code,
    string Name,
    bool IsPercentage,
    decimal Value,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive);

// ── Report DTOs ───────────────────────────────────────────────────────────────

public record RevenueReportDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal TotalRevenue,
    decimal TotalRefunded,
    decimal NetRevenue,
    int PaymentCount,
    int RefundCount,
    IReadOnlyList<RevenueByMethodDto> ByMethod);

public record RevenueByMethodDto(
    Guid? PaymentMethodId,
    string MethodName,
    decimal TotalAmount,
    int Count);

public record PackageReportDto(
    Guid CorporationId,
    int TotalActive,
    int TotalExhausted,
    int TotalExpired,
    int TotalCancelled,
    int ExpiringIn30Days,
    decimal TotalCreditsGranted,
    decimal TotalCreditsConsumed,
    decimal TotalCreditsRemaining,
    IReadOnlyList<PackageSalesSummaryDto> TopPackages);

public record PackageSalesSummaryDto(
    Guid? PackageDefinitionId,
    string PackageName,
    int UnitsSold,
    decimal TotalRevenue);

public record CreditUsageReportDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal TotalGranted,
    decimal TotalConsumed,
    decimal TotalExpired,
    decimal TotalRefunded,
    decimal TotalAdjusted,
    IReadOnlyList<CreditUsageByStudentDto> ByStudent);

public record CreditUsageByStudentDto(
    Guid StudentId,
    string StudentFullName,
    decimal Granted,
    decimal Consumed,
    decimal Remaining);

// ── Projection Helper ─────────────────────────────────────────────────────────

internal static class FinanceProjection
{
    public static async Task<InvoiceDto?> LoadInvoiceAsync(
        IAppDbContext db, Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.Discounts)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice is null) return null;

        string? studentName = null;
        if (invoice.StudentId.HasValue)
        {
            var student = await db.Students.AsNoTracking()
                .Where(s => s.Id == invoice.StudentId.Value)
                .Select(s => new { s.FirstName, s.LastName })
                .FirstOrDefaultAsync(ct);
            studentName = student is null ? null : $"{student.FirstName} {student.LastName}".Trim();
        }

        var paidAmount = invoice.Payments
            .Where(p => p.Status == "captured")
            .Sum(p => p.Amount);

        return ToInvoiceDto(invoice, studentName, paidAmount);
    }

    public static InvoiceDto ToInvoiceDto(Invoice i, string? studentName, decimal paidAmount)
        => new(
            i.Id, i.CorporationId, i.StudentId, studentName,
            i.GuardianId, i.InvoiceNo, i.IssueDate, i.DueDate,
            i.Currency, i.Subtotal, i.DiscountTotal, i.TaxTotal, i.GrandTotal,
            i.Status, i.CreatedAt, i.UpdatedAt, i.RowVersion,
            i.Lines.OrderBy(l => l.SortOrder).Select(ToInvoiceLineDto).ToList(),
            i.Payments.Select(p => ToPaymentDto(p)).ToList(),
            i.Discounts.Select(ToDiscountDto).ToList());

    public static InvoiceLineDto ToInvoiceLineDto(InvoiceLine l)
        => new(l.Id, l.InvoiceId, l.Description, l.StudentPackageId,
               l.Quantity, l.UnitPrice, l.LineTotal, l.SortOrder);

    public static PaymentDto ToPaymentDto(Payment p, string? studentName = null)
        => new(p.Id, p.CorporationId, p.InvoiceId, p.StudentId, studentName,
               p.PaymentMethodId, p.Amount, p.Currency, p.Status,
               p.GatewayProviderId, p.GatewayReference,
               p.PaidAt, p.CreatedAt, p.RowVersion);

    public static DiscountDto ToDiscountDto(Discount d)
        => new(d.Id, d.CorporationId, d.InvoiceId, d.StudentPackageId,
               d.DiscountTypeId, d.IsPercentage, d.Value, d.Reason, d.CreatedAt);

    public static async Task<StudentPackageDto?> LoadStudentPackageAsync(
        IAppDbContext db, Guid packageId, CancellationToken ct)
    {
        var pkg = await db.StudentPackages
            .AsNoTracking()
            .Include(p => p.PackageDefinition)
            .Include(p => p.CreditLedgerEntries)
            .FirstOrDefaultAsync(p => p.Id == packageId, ct);

        if (pkg is null) return null;

        var student = await db.Students.AsNoTracking()
            .Where(s => s.Id == pkg.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);

        var studentName = student is null ? "" : $"{student.FirstName} {student.LastName}".Trim();
        var remaining   = pkg.CreditLedgerEntries.Sum(e => e.Delta);

        return ToStudentPackageDto(pkg, studentName, remaining);
    }

    public static StudentPackageDto ToStudentPackageDto(
        StudentPackage pkg, string studentName, decimal remainingCredits)
        => new(
            pkg.Id, pkg.CorporationId, pkg.StudentId, studentName,
            pkg.PackageDefinitionId, pkg.PackageDefinition?.Name,
            pkg.PurchasedOn, pkg.ExpiresOn,
            pkg.TotalCredits, remainingCredits,
            pkg.Price, pkg.Currency, pkg.Status,
            pkg.CreatedAt, pkg.UpdatedAt, pkg.RowVersion);
}
