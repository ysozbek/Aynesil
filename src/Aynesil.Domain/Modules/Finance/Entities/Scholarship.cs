using Aynesil.Domain.Modules.Finance.Events;

namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// A scholarship granted to a student that reduces their fees by a percentage or fixed amount.
/// scholarship_type_id references ref.ref_value (ref_type 'scholarship_type') — configurable.
///
/// Either percentage or amount must be set — not both and not neither.
///
/// Maps to finance.scholarship.
/// Audit: created_at, updated_at, row_version (no deleted_at, no created_by/updated_by in DDL).
/// </summary>
public class Scholarship : TenantEntity
{
    public Guid StudentId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'scholarship_type'). Configurable.</summary>
    public Guid? ScholarshipTypeId { get; private set; }

    /// <summary>Discount percentage (0–100). Null if fixed-amount scholarship.</summary>
    public decimal? Percentage { get; private set; }

    /// <summary>Fixed monetary discount amount. Null if percentage scholarship.</summary>
    public decimal? Amount { get; private set; }

    public DateOnly? ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }

    /// <summary>FK to iam.user_account — who approved the scholarship.</summary>
    public Guid? ApprovedBy { get; private set; }

    public string? Note { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Scholarship Grant(
        Guid corporationId,
        Guid studentId,
        Guid? scholarshipTypeId = null,
        decimal? percentage = null,
        decimal? amount = null,
        DateOnly? validFrom = null,
        DateOnly? validTo = null,
        Guid? approvedBy = null,
        string? note = null)
    {
        ValidateValues(percentage, amount, validFrom, validTo);

        var scholarship = new Scholarship
        {
            CorporationId     = corporationId,
            StudentId         = studentId,
            ScholarshipTypeId = scholarshipTypeId,
            Percentage        = percentage,
            Amount            = amount,
            ValidFrom         = validFrom,
            ValidTo           = validTo,
            ApprovedBy        = approvedBy,
            Note              = note,
            CreatedAt         = DateTimeOffset.UtcNow,
            UpdatedAt         = DateTimeOffset.UtcNow
        };

        scholarship.AddDomainEvent(new ScholarshipGrantedEvent(
            scholarship.Id, corporationId, studentId, percentage, amount, approvedBy));

        return scholarship;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Update(
        Guid? scholarshipTypeId,
        decimal? percentage,
        decimal? amount,
        DateOnly? validFrom,
        DateOnly? validTo,
        Guid? approvedBy,
        string? note)
    {
        ValidateValues(percentage, amount, validFrom, validTo);

        ScholarshipTypeId = scholarshipTypeId;
        Percentage        = percentage;
        Amount            = amount;
        ValidFrom         = validFrom;
        ValidTo           = validTo;
        ApprovedBy        = approvedBy;
        Note              = note;
        UpdatedAt         = DateTimeOffset.UtcNow;
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static void ValidateValues(
        decimal? percentage, decimal? amount, DateOnly? validFrom, DateOnly? validTo)
    {
        if (percentage is null && amount is null)
            throw new ArgumentException(
                "Scholarship must specify either a percentage or a fixed amount.");

        if (percentage is not null && amount is not null)
            throw new ArgumentException(
                "Scholarship cannot specify both a percentage and a fixed amount.");

        if (percentage is not null && (percentage <= 0 || percentage > 100))
            throw new ArgumentException("Scholarship percentage must be between 0 and 100.");

        if (amount is not null && amount <= 0)
            throw new ArgumentException("Scholarship amount must be positive.");

        if (validFrom.HasValue && validTo.HasValue && validTo < validFrom)
            throw new ArgumentException("Scholarship valid_to must be on or after valid_from.");
    }
}
