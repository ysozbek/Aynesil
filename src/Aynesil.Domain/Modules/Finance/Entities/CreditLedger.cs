using Aynesil.Domain.Modules.Finance.Events;

namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// Append-only financial ledger entry for student package credit movements.
/// Each grant/consumption/refund/adjustment/expiry is one immutable row.
/// The package remaining balance = SUM(delta) over all entries for that package.
///
/// Entry types:
///   grant      (+): initial purchase credit grant or bonus credits
///   consume    (-): session completion consumes credits
///   refund     (+): credits returned when a session is cancelled/refunded
///   adjustment (+/-): manual correction by staff
///   expire     (-): credits lapsed past the package expiry date
///
/// Maps to finance.credit_ledger (append-only, no deleted_at, no updated_at).
/// Financial integrity: rows are NEVER modified after insertion.
/// </summary>
public class CreditLedger : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid StudentPackageId { get; private set; }

    /// <summary>grant | consume | refund | adjustment | expire</summary>
    public string EntryType { get; private set; } = string.Empty;

    /// <summary>Positive for grants/refunds; negative for consume/expire.</summary>
    public decimal Delta { get; private set; }

    /// <summary>FK to scheduling.session — populated when credits are consumed by a session.</summary>
    public Guid? SessionId { get; private set; }

    public string? Reason { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; private set; }

    // ── Factory methods ───────────────────────────────────────────────────────

    internal static CreditLedger Grant(
        Guid corporationId,
        Guid studentPackageId,
        decimal amount,
        string reason,
        Guid? createdBy = null)
    {
        var entry = new CreditLedger
        {
            CorporationId    = corporationId,
            StudentPackageId = studentPackageId,
            EntryType        = "grant",
            Delta            = Math.Abs(amount),
            Reason           = reason,
            OccurredAt       = DateTimeOffset.UtcNow,
            CreatedBy        = createdBy
        };

        entry.AddDomainEvent(new CreditGrantedEvent(
            entry.Id, corporationId, studentPackageId, amount, reason));

        return entry;
    }

    internal static CreditLedger Consume(
        Guid corporationId,
        Guid studentPackageId,
        decimal amount,
        Guid? sessionId = null,
        string? reason = null,
        Guid? createdBy = null)
    {
        var entry = new CreditLedger
        {
            CorporationId    = corporationId,
            StudentPackageId = studentPackageId,
            EntryType        = "consume",
            Delta            = -Math.Abs(amount),
            SessionId        = sessionId,
            Reason           = reason,
            OccurredAt       = DateTimeOffset.UtcNow,
            CreatedBy        = createdBy
        };

        entry.AddDomainEvent(new CreditConsumedEvent(
            entry.Id, corporationId, studentPackageId, amount, sessionId));

        return entry;
    }

    internal static CreditLedger RefundCredits(
        Guid corporationId,
        Guid studentPackageId,
        decimal amount,
        string reason,
        Guid? createdBy = null)
        => new()
        {
            CorporationId    = corporationId,
            StudentPackageId = studentPackageId,
            EntryType        = "refund",
            Delta            = Math.Abs(amount),
            Reason           = reason,
            OccurredAt       = DateTimeOffset.UtcNow,
            CreatedBy        = createdBy
        };

    internal static CreditLedger Expire(
        Guid corporationId,
        Guid studentPackageId,
        decimal amount,
        string reason,
        Guid? createdBy = null)
        => new()
        {
            CorporationId    = corporationId,
            StudentPackageId = studentPackageId,
            EntryType        = "expire",
            Delta            = -Math.Abs(amount),
            Reason           = reason,
            OccurredAt       = DateTimeOffset.UtcNow,
            CreatedBy        = createdBy
        };

    public static CreditLedger Adjust(
        Guid corporationId,
        Guid studentPackageId,
        decimal delta,
        string reason,
        Guid? createdBy = null)
    {
        if (delta == 0)
            throw new ArgumentException("Adjustment delta cannot be zero.");

        return new CreditLedger
        {
            CorporationId    = corporationId,
            StudentPackageId = studentPackageId,
            EntryType        = "adjustment",
            Delta            = delta,
            Reason           = reason,
            OccurredAt       = DateTimeOffset.UtcNow,
            CreatedBy        = createdBy
        };
    }
}
