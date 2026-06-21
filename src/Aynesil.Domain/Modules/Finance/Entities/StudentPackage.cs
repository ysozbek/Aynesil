using Aynesil.Domain.Modules.Finance.Events;

namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// A purchased package instance for a specific student.
/// Remaining credits are derived from SUM(finance.credit_ledger.delta) — never a mutable balance column.
///
/// Status lifecycle: active → exhausted | expired | cancelled
///
/// Maps to finance.student_package.
/// Audit: created_at, created_by, updated_at, deleted_at, row_version (no updated_by in DDL).
/// </summary>
public class StudentPackage : TenantEntity
{
    public Guid StudentId { get; private set; }
    public Guid? PackageDefinitionId { get; private set; }
    public DateOnly PurchasedOn { get; private set; }
    public DateOnly? ExpiresOn { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "TRY";

    /// <summary>active | exhausted | expired | cancelled</summary>
    public string Status { get; private set; } = "active";

    // ── Navigations ───────────────────────────────────────────────────────────

    public PackageDefinition? PackageDefinition { get; private set; }
    public ICollection<CreditLedger> CreditLedgerEntries { get; private set; } = [];
    public ICollection<InvoiceLine> InvoiceLines { get; private set; } = [];
    public ICollection<Discount> Discounts { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static StudentPackage Purchase(
        Guid corporationId,
        Guid studentId,
        decimal totalCredits,
        decimal price,
        Guid? packageDefinitionId = null,
        DateOnly? expiresOn = null,
        string currency = "TRY",
        Guid? purchasedBy = null)
    {
        if (totalCredits <= 0)
            throw new ArgumentException("Package must include at least one credit.");

        if (price < 0)
            throw new ArgumentException("Package price cannot be negative.");

        var pkg = new StudentPackage
        {
            CorporationId       = corporationId,
            StudentId           = studentId,
            PackageDefinitionId = packageDefinitionId,
            PurchasedOn         = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpiresOn           = expiresOn,
            TotalCredits        = totalCredits,
            Price               = price,
            Currency            = currency,
            Status              = "active",
            CreatedAt           = DateTimeOffset.UtcNow,
            CreatedBy           = purchasedBy,
            UpdatedAt           = DateTimeOffset.UtcNow
        };

        pkg.AddDomainEvent(new PackagePurchasedEvent(
            pkg.Id, corporationId, studentId, totalCredits, price, packageDefinitionId, purchasedBy));

        return pkg;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void MarkExhausted()
    {
        EnsureActive();
        Status    = "exhausted";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkExpired()
    {
        EnsureActive();
        Status    = "expired";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(Guid? cancelledBy = null)
    {
        if (Status is "exhausted" or "expired")
            throw new InvalidOperationException(
                $"Cannot cancel a package in '{Status}' status.");

        Status    = "cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public CreditLedger GrantCredits(decimal amount, string reason, Guid? grantedBy = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit grant amount must be positive.");

        return CreditLedger.Grant(CorporationId, Id, amount, reason, grantedBy);
    }

    public CreditLedger ConsumeCredits(
        decimal amount,
        Guid? sessionId = null,
        string? reason = null,
        Guid? consumedBy = null)
    {
        EnsureActive();

        if (amount <= 0)
            throw new ArgumentException("Credit consumption amount must be positive.");

        return CreditLedger.Consume(CorporationId, Id, amount, sessionId, reason, consumedBy);
    }

    public CreditLedger RefundCredits(decimal amount, string reason, Guid? refundedBy = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit refund amount must be positive.");

        return CreditLedger.RefundCredits(CorporationId, Id, amount, reason, refundedBy);
    }

    public CreditLedger ExpireCredits(decimal amount, string reason, Guid? expiredBy = null)
        => CreditLedger.Expire(CorporationId, Id, amount, reason, expiredBy);

    // ── Invariants ────────────────────────────────────────────────────────────

    private void EnsureActive()
    {
        if (Status != "active")
            throw new InvalidOperationException(
                $"Package is in '{Status}' status. Only active packages accept credit operations.");
    }
}
