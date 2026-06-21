namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// A promotional campaign code that applies a discount at purchase time.
/// Codes are unique per corporation. Redemption tracking is done in the application layer.
///
/// Maps to finance.promotion.
/// No standard audit fields in DDL.
/// </summary>
public class Promotion : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    /// <summary>True = percentage discount; False = fixed amount.</summary>
    public bool IsPercentage { get; private set; } = true;

    /// <summary>Percentage (0–100) or fixed amount in currency, depending on IsPercentage.</summary>
    public decimal Value { get; private set; }

    public DateOnly? ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }

    /// <summary>Maximum total redemptions allowed. Null = unlimited.</summary>
    public int? MaxRedemptions { get; private set; }

    public bool IsActive { get; private set; } = true;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Promotion Create(
        Guid corporationId,
        string code,
        string name,
        decimal value,
        bool isPercentage = true,
        DateOnly? validFrom = null,
        DateOnly? validTo = null,
        int? maxRedemptions = null)
    {
        ValidateValue(value, isPercentage);

        if (validFrom.HasValue && validTo.HasValue && validTo < validFrom)
            throw new ArgumentException("Promotion valid_to must be on or after valid_from.");

        return new Promotion
        {
            CorporationId  = corporationId,
            Code           = code,
            Name           = name,
            IsPercentage   = isPercentage,
            Value          = value,
            ValidFrom      = validFrom,
            ValidTo        = validTo,
            MaxRedemptions = maxRedemptions,
            IsActive       = true
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void UpdateDetails(
        string code,
        string name,
        decimal value,
        bool isPercentage,
        DateOnly? validFrom,
        DateOnly? validTo,
        int? maxRedemptions)
    {
        ValidateValue(value, isPercentage);

        Code           = code;
        Name           = name;
        IsPercentage   = isPercentage;
        Value          = value;
        ValidFrom      = validFrom;
        ValidTo        = validTo;
        MaxRedemptions = maxRedemptions;
    }

    public void Activate()   => IsActive = true;
    public void Deactivate() => IsActive = false;

    public bool IsValidOn(DateOnly date)
        => IsActive
           && (ValidFrom is null || date >= ValidFrom.Value)
           && (ValidTo is null   || date <= ValidTo.Value);

    // ── Invariants ────────────────────────────────────────────────────────────

    private static void ValidateValue(decimal value, bool isPercentage)
    {
        if (value <= 0)
            throw new ArgumentException("Promotion value must be positive.");

        if (isPercentage && value > 100)
            throw new ArgumentException("Percentage promotion cannot exceed 100%.");
    }
}
