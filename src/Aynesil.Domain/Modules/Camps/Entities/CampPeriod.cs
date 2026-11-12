using Aynesil.Domain.Common;
namespace Aynesil.Domain.Modules.Camps.Entities;

/// <summary>
/// Maps to camps.camp_period.
/// A time-bounded period within a camp (e.g. "Week 1: 7–13 Jul 2025").
/// The DB schema has no audit columns — this entity extends BaseEntity only.
/// capacity: maximum enrollments for this specific period (may differ from Camp.Capacity).
/// </summary>
public class CampPeriod : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid CampId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int? Capacity { get; private set; }

    public Camp Camp { get; private set; } = null!;
    public ICollection<CampEnrollment> Enrollments { get; private set; } = [];
    public ICollection<CampActivity> Activities { get; private set; } = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static CampPeriod Create(
        Guid corporationId,
        Guid campId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        int? capacity = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidArgumentException(MsgKeys.Validation.Required, MsgArg.Field("Name"));
        if (endDate < startDate)
            throw new InvalidArgumentException(MsgKeys.Validation.MustNotBeBefore, MsgArg.Field("EndDate"), MsgArg.Field("StartDate"));
        if (capacity is <= 0)
            throw new InvalidArgumentException(MsgKeys.Validation.MustBePositive, MsgArg.Field("Capacity"));

        return new CampPeriod
        {
            CorporationId = corporationId,
            CampId        = campId,
            Name          = name.Trim(),
            StartDate     = startDate,
            EndDate       = endDate,
            Capacity      = capacity
        };
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(string name, DateOnly startDate, DateOnly endDate, int? capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidArgumentException(MsgKeys.Validation.Required, MsgArg.Field("Name"));
        if (endDate < startDate)
            throw new InvalidArgumentException(MsgKeys.Validation.MustNotBeBefore, MsgArg.Field("EndDate"), MsgArg.Field("StartDate"));
        if (capacity is <= 0)
            throw new InvalidArgumentException(MsgKeys.Validation.MustBePositive, MsgArg.Field("Capacity"));

        Name      = name.Trim();
        StartDate = startDate;
        EndDate   = endDate;
        Capacity  = capacity;
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    public bool IsAtCapacity() =>
        Capacity.HasValue && Enrollments.Count(e => e.Status == "enrolled") >= Capacity.Value;

    public int AvailableSlots() =>
        Capacity.HasValue
            ? Math.Max(0, Capacity.Value - Enrollments.Count(e => e.Status == "enrolled"))
            : int.MaxValue;
}
