using Aynesil.Domain.Modules.Camps.Events;

namespace Aynesil.Domain.Modules.Camps.Entities;

/// <summary>
/// Maps to camps.camp_activity_participation.
/// Tracks an enrolled student's participation in a specific camp activity.
/// Status workflow: registered → attended | absent | excused.
/// Extends BaseEntity — no soft-delete columns in DDL.
/// </summary>
public class CampActivityParticipation : BaseEntity
{
    private static readonly string[] ValidStatuses =
        ["registered", "attended", "absent", "excused"];

    public Guid CorporationId { get; private set; }
    public Guid CampActivityId { get; private set; }
    public Guid CampEnrollmentId { get; private set; }

    /// <summary>'registered' | 'attended' | 'absent' | 'excused'</summary>
    public string Status { get; private set; } = "registered";

    public string? Notes { get; private set; }
    public Guid? RecordedBy { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; } = DateTimeOffset.UtcNow;

    public CampActivity Activity { get; private set; } = null!;
    public CampEnrollment Enrollment { get; private set; } = null!;

    public static CampActivityParticipation Register(
        Guid corporationId,
        Guid campActivityId,
        Guid campEnrollmentId,
        string status = "registered",
        string? notes = null,
        Guid? recordedBy = null)
    {
        ValidateStatus(status);

        var participation = new CampActivityParticipation
        {
            CorporationId    = corporationId,
            CampActivityId   = campActivityId,
            CampEnrollmentId = campEnrollmentId,
            Status           = status,
            Notes            = notes?.Trim(),
            RecordedBy       = recordedBy,
            RecordedAt       = DateTimeOffset.UtcNow
        };

        participation.AddDomainEvent(new CampActivityParticipationRecordedEvent(
            participation.Id, corporationId, campActivityId, campEnrollmentId, status));

        return participation;
    }

    public void UpdateStatus(string status, string? notes = null, Guid? recordedBy = null)
    {
        ValidateStatus(status);
        Status     = status;
        Notes      = notes?.Trim() ?? Notes;
        RecordedBy = recordedBy ?? RecordedBy;
        RecordedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException(
                $"Invalid participation status '{status}'. Valid: {string.Join(", ", ValidStatuses)}");
    }
}
