using Aynesil.Domain.Modules.Scheduling.Events;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// A single schedulable unit — individual, group, intensive, camp, online, makeup, or any
/// tenant-defined session type. session_type_id is configurable via ref.ref_value.
///
/// Room double-booking is prevented by a DB-level EXCLUDE USING GIST constraint.
/// Educator double-booking is prevented by a trigger (see 99_triggers_rls_policies.sql).
///
/// Status lifecycle: scheduled → completed | cancelled | no_show | rescheduled
///
/// Maps to scheduling.session.
/// Full audit: created_at, created_by, updated_at, updated_by, deleted_at, row_version.
/// </summary>
public class Session : TenantEntity
{
    public Guid? CampusId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'session_type'). Configurable.</summary>
    public Guid SessionTypeId { get; private set; }

    public Guid? RoomId { get; private set; }
    public Guid? RecurringScheduleId { get; private set; }
    public Guid? ProgramServiceId { get; private set; }
    public string? Title { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>scheduled | completed | cancelled | no_show | rescheduled</summary>
    public string Status { get; private set; } = "scheduled";

    /// <summary>True for makeup sessions.</summary>
    public bool IsMakeup { get; private set; }

    public string? CancelReason { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public Room? Room { get; private set; }
    public ICollection<SessionParticipant> Participants { get; private set; } = [];
    public ICollection<SessionEducator> Educators { get; private set; } = [];
    public ICollection<SessionGoal> Goals { get; private set; } = [];
    public ICollection<SessionNote> Notes { get; private set; } = [];
    public ICollection<Attendance> Attendances { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Session Schedule(
        Guid corporationId,
        Guid sessionTypeId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid? campusId = null,
        Guid? roomId = null,
        Guid? recurringScheduleId = null,
        Guid? programServiceId = null,
        string? title = null,
        bool isMakeup = false,
        Guid? createdBy = null)
    {
        if (endsAt <= startsAt)
            throw new ArgumentException("Session ends_at must be after starts_at.");

        var session = new Session
        {
            CorporationId       = corporationId,
            CampusId            = campusId,
            SessionTypeId       = sessionTypeId,
            RoomId              = roomId,
            RecurringScheduleId = recurringScheduleId,
            ProgramServiceId    = programServiceId,
            Title               = title,
            StartsAt            = startsAt,
            EndsAt              = endsAt,
            Status              = "scheduled",
            IsMakeup            = isMakeup,
            CreatedAt           = DateTimeOffset.UtcNow,
            CreatedBy           = createdBy,
            UpdatedAt           = DateTimeOffset.UtcNow
        };

        session.AddDomainEvent(new SessionCreatedEvent(
            session.Id, corporationId, campusId, sessionTypeId,
            startsAt, endsAt, isMakeup, createdBy));

        return session;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Reschedule(
        DateTimeOffset newStartsAt,
        DateTimeOffset newEndsAt,
        Guid? roomId,
        Guid? updatedBy = null)
    {
        EnsureNotTerminal();

        if (newEndsAt <= newStartsAt)
            throw new ArgumentException("Session ends_at must be after starts_at.");

        var previous = Status;
        StartsAt  = newStartsAt;
        EndsAt    = newEndsAt;
        RoomId    = roomId;
        Status    = "rescheduled";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new SessionStatusChangedEvent(
            Id, CorporationId, previous, Status, updatedBy));
    }

    public void Complete(Guid? updatedBy = null)
    {
        EnsureTransitionAllowed("completed");

        var previous = Status;
        Status    = "completed";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new SessionStatusChangedEvent(
            Id, CorporationId, previous, Status, updatedBy));
    }

    public void Cancel(string? reason, Guid? updatedBy = null)
    {
        EnsureTransitionAllowed("cancelled");

        var previous  = Status;
        Status        = "cancelled";
        CancelReason  = reason;
        UpdatedAt     = DateTimeOffset.UtcNow;
        UpdatedBy     = updatedBy;

        AddDomainEvent(new SessionStatusChangedEvent(
            Id, CorporationId, previous, Status, updatedBy));
    }

    public void MarkNoShow(Guid? updatedBy = null)
    {
        EnsureTransitionAllowed("no_show");

        var previous = Status;
        Status    = "no_show";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new SessionStatusChangedEvent(
            Id, CorporationId, previous, Status, updatedBy));
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static readonly string[] TerminalStatuses = ["cancelled", "completed", "no_show"];

    private void EnsureNotTerminal()
    {
        if (TerminalStatuses.Contains(Status))
            throw new InvalidOperationException(
                $"Session is in terminal status '{Status}' and cannot be modified.");
    }

    private void EnsureTransitionAllowed(string target)
    {
        // cancelled and no_show sessions cannot transition further
        if (Status == "cancelled")
            throw new InvalidOperationException("Cannot transition a cancelled session.");

        if (Status == "no_show")
            throw new InvalidOperationException("Cannot transition a no-show session.");

        if (Status == "completed" && target != "completed")
            throw new InvalidOperationException("Completed sessions cannot change status.");
    }
}
