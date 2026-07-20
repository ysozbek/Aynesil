namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.leave_balance.
/// Tracks an educator's leave entitlement and usage per leave type per calendar year.
/// Unique constraint (DB): (educator_id, leave_type_id, period_year).
///
/// Balance deduction occurs on leave approval; restoration on approved-leave cancellation.
/// Leave types without a balance record are treated as unlimited (no validation enforced).
///
/// DDL note: table has no audit columns. All AuditableEntity fields are ignored in EF config.
/// </summary>
public class LeaveBalance : TenantEntity
{
    public Guid EducatorId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'leave_type'). Configurable.</summary>
    public Guid? LeaveTypeId { get; private set; }

    public int PeriodYear { get; private set; }

    /// <summary>Total entitled leave (days or hours) for this period.</summary>
    public decimal Entitled { get; private set; }

    /// <summary>Total consumed leave (days or hours) for this period.</summary>
    public decimal Used { get; private set; }

    /// <summary>'day' | 'hour'</summary>
    public string Unit { get; private set; } = "day";

    /// <summary>Remaining balance = Entitled - Used.</summary>
    public decimal Remaining => Entitled - Used;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static LeaveBalance Initialize(
        Guid corporationId,
        Guid educatorId,
        Guid? leaveTypeId,
        int periodYear,
        decimal entitled,
        string unit = "day")
    {
        if (!ValidUnits.Contains(unit))
            throw new ArgumentException($"Invalid unit '{unit}'. Must be: day, hour.");

        if (entitled < 0)
            throw new ArgumentException("Entitled leave must be non-negative.");

        return new LeaveBalance
        {
            CorporationId = corporationId,
            EducatorId    = educatorId,
            LeaveTypeId   = leaveTypeId,
            PeriodYear    = periodYear,
            Entitled      = entitled,
            Used          = 0,
            Unit          = unit
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Consume(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Consume amount must be positive.");

        if (Used + amount > Entitled)
            throw new InvalidOperationException(
                $"Insufficient leave balance. Entitled: {Entitled} {Unit}, Used: {Used} {Unit}, Requested: {amount} {Unit}.");

        Used += amount;
    }

    public void Restore(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Restore amount must be positive.");

        Used = Math.Max(0, Used - amount);
    }

    public void AdjustEntitlement(decimal entitled)
    {
        if (entitled < 0)
            throw new ArgumentException("Entitled leave must be non-negative.");

        Entitled = entitled;
    }

    public void CarryForward(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Carry-forward amount must be non-negative.");

        Entitled += amount;
    }

    private static readonly string[] ValidUnits = ["day", "hour"];
}
