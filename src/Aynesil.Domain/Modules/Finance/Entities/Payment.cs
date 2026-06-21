using Aynesil.Domain.Modules.Finance.Events;

namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// A payment record against an invoice.
/// Financial immutability rule: payment rows are never physically deleted.
/// Corrections (overpayments, cancellations) are done via Refund records.
///
/// payment_method_id references ref.ref_value (ref_type 'payment_method') — configurable.
/// gateway_provider_id references core.integration_connection — payment gateway seam.
///
/// Status lifecycle: pending → authorized → captured | failed; captured → refunded
///
/// Maps to finance.payment.
/// Audit: created_at, updated_at, row_version (no deleted_at — financial immutability;
///   no created_by / updated_by in DDL).
/// </summary>
public class Payment : TenantEntity
{
    public Guid? InvoiceId { get; private set; }
    public Guid? StudentId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'payment_method'). Configurable.</summary>
    public Guid? PaymentMethodId { get; private set; }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";

    /// <summary>pending | authorized | captured | failed | refunded</summary>
    public string Status { get; private set; } = "pending";

    /// <summary>FK to core.integration_connection — payment gateway seam.</summary>
    public Guid? GatewayProviderId { get; private set; }

    public string? GatewayReference { get; private set; }

    /// <summary>Gateway callback deduplication key.</summary>
    public string? IdempotencyKey { get; private set; }

    public DateTimeOffset? PaidAt { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<Refund> Refunds { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Payment Record(
        Guid corporationId,
        decimal amount,
        string currency = "TRY",
        Guid? invoiceId = null,
        Guid? studentId = null,
        Guid? paymentMethodId = null,
        Guid? gatewayProviderId = null,
        string? gatewayReference = null,
        string? idempotencyKey = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive.");

        return new Payment
        {
            CorporationId     = corporationId,
            InvoiceId         = invoiceId,
            StudentId         = studentId,
            PaymentMethodId   = paymentMethodId,
            Amount            = amount,
            Currency          = currency,
            Status            = "pending",
            GatewayProviderId = gatewayProviderId,
            GatewayReference  = gatewayReference,
            IdempotencyKey    = idempotencyKey,
            CreatedAt         = DateTimeOffset.UtcNow,
            UpdatedAt         = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Authorize(string? gatewayReference = null)
    {
        if (Status != "pending")
            throw new InvalidOperationException("Only pending payments can be authorized.");

        Status           = "authorized";
        GatewayReference = gatewayReference ?? GatewayReference;
        UpdatedAt        = DateTimeOffset.UtcNow;
    }

    public void Capture(string? gatewayReference = null, DateTimeOffset? paidAt = null)
    {
        if (Status is not ("pending" or "authorized"))
            throw new InvalidOperationException("Only pending or authorized payments can be captured.");

        Status           = "captured";
        GatewayReference = gatewayReference ?? GatewayReference;
        PaidAt           = paidAt ?? DateTimeOffset.UtcNow;
        UpdatedAt        = DateTimeOffset.UtcNow;

        AddDomainEvent(new PaymentCapturedEvent(
            Id, CorporationId, InvoiceId, StudentId, Amount, Currency));
    }

    public void Fail()
    {
        if (Status is "captured" or "refunded")
            throw new InvalidOperationException($"Cannot fail a '{Status}' payment.");

        Status    = "failed";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRefunded()
    {
        if (Status != "captured")
            throw new InvalidOperationException("Only captured payments can be marked as refunded.");

        Status    = "refunded";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
