using Aynesil.Domain.Modules.Finance.Events;

namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// A refund against a captured payment.
/// Refund rows are immutable once processed — financial audit history must be preserved.
/// Physical deletion is not allowed.
///
/// Status lifecycle: pending → processed | failed
///
/// Maps to finance.refund.
/// Audit: created_at only (immutable after insertion).
/// </summary>
public class Refund : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid PaymentId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Reason { get; private set; }

    /// <summary>pending | processed | failed</summary>
    public string Status { get; private set; } = "pending";

    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Refund Request(
        Guid corporationId,
        Guid paymentId,
        decimal amount,
        string? reason = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Refund amount must be positive.");

        return new Refund
        {
            CorporationId = corporationId,
            PaymentId     = paymentId,
            Amount        = amount,
            Reason        = reason,
            Status        = "pending",
            CreatedAt     = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Process()
    {
        if (Status != "pending")
            throw new InvalidOperationException("Only pending refunds can be processed.");

        Status      = "processed";
        ProcessedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new RefundProcessedEvent(Id, CorporationId, PaymentId, Amount));
    }

    public void Fail()
    {
        if (Status != "pending")
            throw new InvalidOperationException("Only pending refunds can be marked as failed.");

        Status = "failed";
    }
}
