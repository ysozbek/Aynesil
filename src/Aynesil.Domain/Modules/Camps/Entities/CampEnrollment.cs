using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Camps.Events;

namespace Aynesil.Domain.Modules.Camps.Entities;

/// <summary>
/// Maps to camps.camp_enrollment.
/// Links a student to a specific camp period.
/// Status workflow: enrolled ↔ waitlist → withdrawn | completed.
/// The DB schema has no standard audit columns — this entity extends BaseEntity only.
/// </summary>
public class CampEnrollment : BaseEntity
{
    private static readonly string[] ValidStatuses = ["enrolled", "waitlist", "withdrawn", "completed"];

    public Guid CorporationId { get; private set; }
    public Guid CampPeriodId { get; private set; }
    public Guid StudentId { get; private set; }

    /// <summary>Optional link to the finance package that covers this camp enrollment.</summary>
    public Guid? StudentPackageId { get; private set; }

    /// <summary>'enrolled' | 'waitlist' | 'withdrawn' | 'completed'</summary>
    public string Status { get; private set; } = "enrolled";

    public DateTimeOffset EnrolledAt { get; private set; } = DateTimeOffset.UtcNow;

    public CampPeriod Period { get; private set; } = null!;
    public ICollection<CampAttendance> Attendances { get; private set; } = [];
    public ICollection<CampReport> Reports { get; private set; } = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static CampEnrollment Create(
        Guid corporationId,
        Guid campPeriodId,
        Guid studentId,
        string status = "enrolled",
        Guid? studentPackageId = null)
    {
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException(
                $"Invalid enrollment status '{status}'. Valid: {string.Join(", ", ValidStatuses)}");

        var enrollment = new CampEnrollment
        {
            CorporationId    = corporationId,
            CampPeriodId     = campPeriodId,
            StudentId        = studentId,
            StudentPackageId = studentPackageId,
            Status           = status,
            EnrolledAt       = DateTimeOffset.UtcNow
        };

        enrollment.AddDomainEvent(new CampEnrolledEvent(
            enrollment.Id, campPeriodId, studentId, corporationId, status));

        return enrollment;
    }

    // ── Workflow Transitions ───────────────────────────────────────────────────

    public void MoveToWaitlist()
    {
        EnsureNotTerminal();
        Status = "waitlist";
    }

    public void Enroll()
    {
        if (Status != "waitlist")
            throw new BusinessRuleViolationException(MsgKeys.Rule.OnlyWaitlistedToEnrolled);

        Status = "enrolled";
        AddDomainEvent(new CampEnrolledEvent(
            Id, CampPeriodId, StudentId, CorporationId, Status));
    }

    public void Withdraw()
    {
        EnsureNotTerminal();
        Status = "withdrawn";
        AddDomainEvent(new CampEnrollmentWithdrawnEvent(
            Id, CampPeriodId, StudentId, CorporationId));
    }

    public void Complete()
    {
        if (Status != "enrolled")
            throw new BusinessRuleViolationException(MsgKeys.Rule.OnlyEnrolledCanComplete);

        Status = "completed";
        AddDomainEvent(new CampEnrollmentCompletedEvent(
            Id, CampPeriodId, StudentId, CorporationId));
    }

    public void UpdatePackage(Guid? studentPackageId) =>
        StudentPackageId = studentPackageId;

    private void EnsureNotTerminal()
    {
        if (Status is "withdrawn" or "completed")
            throw new BusinessRuleViolationException(MsgKeys.Rule.EnrollmentChangeStatus, new EnumValue("Status", Status));
    }
}
