namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// A discount record applied to an invoice or student package.
/// Discounts are immutable financial records — history must be fully preserved.
/// discount_type_id references ref.ref_value (ref_type 'discount_type') — configurable.
///
/// Supports both percentage discounts (is_percentage = true) and fixed-amount discounts.
///
/// Maps to finance.discount.
/// Audit: created_at only (immutable after insertion).
/// </summary>
public class Discount : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public Guid? StudentPackageId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'discount_type'). Configurable.</summary>
    public Guid? DiscountTypeId { get; private set; }

    /// <summary>True = percentage discount; False = fixed amount.</summary>
    public bool IsPercentage { get; private set; } = true;

    /// <summary>Percentage (0–100) or fixed amount in currency, depending on IsPercentage.</summary>
    public decimal Value { get; private set; }

    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Discount Apply(
        Guid corporationId,
        decimal value,
        bool isPercentage = true,
        Guid? invoiceId = null,
        Guid? studentPackageId = null,
        Guid? discountTypeId = null,
        string? reason = null)
    {
        if (value <= 0)
            throw new ArgumentException("Discount value must be positive.");

        if (isPercentage && value > 100)
            throw new ArgumentException("Percentage discount cannot exceed 100%.");

        return new Discount
        {
            CorporationId    = corporationId,
            InvoiceId        = invoiceId,
            StudentPackageId = studentPackageId,
            DiscountTypeId   = discountTypeId,
            IsPercentage     = isPercentage,
            Value            = value,
            Reason           = reason,
            CreatedAt        = DateTimeOffset.UtcNow
        };
    }
}
