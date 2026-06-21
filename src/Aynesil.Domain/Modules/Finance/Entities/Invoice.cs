using Aynesil.Domain.Modules.Finance.Events;

namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// Financial invoice for a student/guardian.
/// Totals (subtotal, discount_total, grand_total) are recalculated from lines and applied discounts.
///
/// Status lifecycle: draft → issued → paid | partial | overdue | void
///
/// Maps to finance.invoice.
/// Audit: created_at, updated_at, deleted_at, row_version (no created_by / updated_by in DDL).
/// Financial integrity: void creates an accounting reversal — physical delete is not allowed.
/// </summary>
public class Invoice : TenantEntity
{
    public Guid? StudentId { get; private set; }
    public Guid? GuardianId { get; private set; }
    public string? InvoiceNo { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }

    /// <summary>draft | issued | paid | partial | void | overdue</summary>
    public string Status { get; private set; } = "draft";

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<InvoiceLine> Lines { get; private set; } = [];
    public ICollection<Payment> Payments { get; private set; } = [];
    public ICollection<Discount> Discounts { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Invoice Create(
        Guid corporationId,
        DateOnly issueDate,
        string currency = "TRY",
        Guid? studentId = null,
        Guid? guardianId = null,
        DateOnly? dueDate = null)
    {
        return new Invoice
        {
            CorporationId = corporationId,
            StudentId     = studentId,
            GuardianId    = guardianId,
            IssueDate     = issueDate,
            DueDate       = dueDate,
            Currency      = currency,
            Status        = "draft",
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void AssignNumber(string invoiceNo)
    {
        if (Status != "draft")
            throw new InvalidOperationException("Cannot assign a number to a non-draft invoice.");

        InvoiceNo = invoiceNo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecalculateTotals()
    {
        Subtotal      = Lines.Sum(l => l.LineTotal);
        DiscountTotal = Discounts.Sum(d =>
            d.IsPercentage ? Math.Round(Subtotal * d.Value / 100m, 2) : d.Value);
        GrandTotal    = Subtotal - DiscountTotal + TaxTotal;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void Issue()
    {
        if (Status != "draft")
            throw new InvalidOperationException("Only draft invoices can be issued.");

        Status    = "issued";
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new InvoiceIssuedEvent(Id, CorporationId, StudentId, GrandTotal, Currency));
    }

    public void MarkPaid()
    {
        EnsureModifiable("paid");
        Status    = "paid";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPartiallyPaid()
    {
        EnsureModifiable("partial");
        Status    = "partial";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkOverdue()
    {
        if (Status is not ("issued" or "partial"))
            throw new InvalidOperationException("Only issued or partial invoices can become overdue.");

        Status    = "overdue";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Void()
    {
        if (Status == "void")
            throw new InvalidOperationException("Invoice is already void.");

        if (Status == "paid")
            throw new InvalidOperationException(
                "Paid invoices cannot be voided directly. Process a refund first.");

        Status    = "void";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private void EnsureModifiable(string target)
    {
        if (Status == "void")
            throw new InvalidOperationException("Cannot transition a void invoice.");

        if (Status == "paid" && target != "paid")
            throw new InvalidOperationException("Cannot re-transition a fully paid invoice.");
    }
}
