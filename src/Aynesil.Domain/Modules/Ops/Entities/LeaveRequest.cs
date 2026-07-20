namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.leave_request.
/// An educator leave request with configurable type (leave_type ref_value) and unit (day|hour).
/// Status lifecycle: pending → approved | rejected | cancelled.
///
/// Overlap prevention is enforced by the DB EXCLUDE USING GIST constraint
/// (educator_id with =, time_range with &&) where status in ('pending','approved').
/// The application layer should also validate for a friendly error.
///
/// DDL note: no deleted_at, no updated_by columns — both are ignored in EF configuration.
/// </summary>
public class LeaveRequest : TenantEntity
{
    public Guid EducatorId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'leave_type'). Configurable.</summary>
    public Guid? LeaveTypeId { get; private set; }

    /// <summary>'day' | 'hour'</summary>
    public string Unit { get; private set; } = "day";

    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Number of days or hours requested, depending on Unit. Null = full period.</summary>
    public decimal? Quantity { get; private set; }

    public string? Reason { get; private set; }

    /// <summary>'pending' | 'approved' | 'rejected' | 'cancelled'</summary>
    public string Status { get; private set; } = "pending";

    public ICollection<LeaveApproval> Approvals { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static LeaveRequest Submit(
        Guid corporationId,
        Guid educatorId,
        Guid? leaveTypeId,
        string unit,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        decimal? quantity,
        string? reason,
        Guid? createdBy = null)
    {
        if (!ValidUnits.Contains(unit))
            throw new ArgumentException($"Invalid unit '{unit}'. Must be: day, hour.");

        if (endsAt <= startsAt)
            throw new ArgumentException("EndsAt must be after StartsAt.");

        if (quantity.HasValue && quantity.Value <= 0)
            throw new ArgumentException("Quantity must be positive.");

        return new LeaveRequest
        {
            CorporationId = corporationId,
            EducatorId    = educatorId,
            LeaveTypeId   = leaveTypeId,
            Unit          = unit,
            StartsAt      = startsAt,
            EndsAt        = endsAt,
            Quantity      = quantity,
            Reason        = reason,
            Status        = "pending",
            CreatedBy     = createdBy
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Update(
        Guid? leaveTypeId,
        string unit,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        decimal? quantity,
        string? reason)
    {
        if (Status != "pending")
            throw new InvalidOperationException("Only pending leave requests can be updated.");

        if (!ValidUnits.Contains(unit))
            throw new ArgumentException($"Invalid unit '{unit}'. Must be: day, hour.");

        if (endsAt <= startsAt)
            throw new ArgumentException("EndsAt must be after StartsAt.");

        if (quantity.HasValue && quantity.Value <= 0)
            throw new ArgumentException("Quantity must be positive.");

        LeaveTypeId = leaveTypeId;
        Unit        = unit;
        StartsAt    = startsAt;
        EndsAt      = endsAt;
        Quantity    = quantity;
        Reason      = reason;
        UpdatedAt   = DateTimeOffset.UtcNow;
    }

    public void Approve()
    {
        if (Status != "pending")
            throw new InvalidOperationException($"Cannot approve a leave request with status '{Status}'.");

        Status    = "approved";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject()
    {
        if (Status != "pending")
            throw new InvalidOperationException($"Cannot reject a leave request with status '{Status}'.");

        Status    = "rejected";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status != "pending" && Status != "approved")
            throw new InvalidOperationException($"Cannot cancel a leave request with status '{Status}'.");

        Status    = "cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns true if this request's time range overlaps with another range.</summary>
    public bool OverlapsWith(DateTimeOffset otherStart, DateTimeOffset otherEnd)
        => StartsAt < otherEnd && EndsAt > otherStart;

    private static readonly string[] ValidUnits = ["day", "hour"];
}
