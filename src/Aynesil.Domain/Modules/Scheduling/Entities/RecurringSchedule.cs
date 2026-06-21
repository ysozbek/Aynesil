using Aynesil.Domain.Modules.Scheduling.Events;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// A recurrence rule that drives the bulk generation of concrete session rows.
/// Supports weekly, biweekly, and monthly patterns with weekday/monthday selectors.
///
/// The materialisation job (Hangfire) reads active rules and creates scheduling.session rows
/// for the materialization window. Exceptions are stored in recurrence_exception.
///
/// Maps to scheduling.recurring_schedule.
/// Audit: created_at, created_by, updated_at, row_version (no updated_by / deleted_at in DDL).
/// </summary>
public class RecurringSchedule : TenantEntity
{
    public Guid? CampusId { get; private set; }
    public Guid? StudentProgramId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'session_type'). Configurable.</summary>
    public Guid? SessionTypeId { get; private set; }

    public Guid? RoomId { get; private set; }

    /// <summary>weekly | biweekly | monthly</summary>
    public string Frequency { get; private set; } = "weekly";

    /// <summary>Recurrence interval (e.g. every N weeks/months).</summary>
    public int IntervalCount { get; private set; } = 1;

    /// <summary>Days of the week for weekly/biweekly rules (0=Sunday … 6=Saturday).</summary>
    public int[]? ByWeekday { get; private set; }

    /// <summary>Days of the month for monthly rules.</summary>
    public int[]? ByMonthday { get; private set; }

    /// <summary>Time of day when the session starts (local time).</summary>
    public TimeOnly StartTime { get; private set; }

    public int DurationMinutes { get; private set; }
    public DateOnly RangeStart { get; private set; }
    public DateOnly? RangeEnd { get; private set; }
    public int? MaxOccurrences { get; private set; }
    public bool IsActive { get; private set; } = true;

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<RecurrenceException> Exceptions { get; private set; } = [];
    public ICollection<Session> Sessions { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static RecurringSchedule Create(
        Guid corporationId,
        string frequency,
        TimeOnly startTime,
        int durationMinutes,
        DateOnly rangeStart,
        Guid? campusId = null,
        Guid? studentProgramId = null,
        Guid? sessionTypeId = null,
        Guid? roomId = null,
        int intervalCount = 1,
        int[]? byWeekday = null,
        int[]? byMonthday = null,
        DateOnly? rangeEnd = null,
        int? maxOccurrences = null,
        Guid? createdBy = null)
    {
        ValidateFrequency(frequency);

        if (durationMinutes <= 0)
            throw new ArgumentException("Duration must be positive.");

        if (rangeEnd.HasValue && rangeEnd.Value <= rangeStart)
            throw new ArgumentException("Range end must be after range start.");

        var schedule = new RecurringSchedule
        {
            CorporationId    = corporationId,
            CampusId         = campusId,
            StudentProgramId = studentProgramId,
            SessionTypeId    = sessionTypeId,
            RoomId           = roomId,
            Frequency        = frequency,
            IntervalCount    = intervalCount,
            ByWeekday        = byWeekday,
            ByMonthday       = byMonthday,
            StartTime        = startTime,
            DurationMinutes  = durationMinutes,
            RangeStart       = rangeStart,
            RangeEnd         = rangeEnd,
            MaxOccurrences   = maxOccurrences,
            IsActive         = true,
            CreatedAt        = DateTimeOffset.UtcNow,
            CreatedBy        = createdBy,
            UpdatedAt        = DateTimeOffset.UtcNow
        };

        schedule.AddDomainEvent(new RecurringScheduleCreatedEvent(
            schedule.Id, corporationId, frequency, rangeStart, rangeEnd, createdBy));

        return schedule;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Deactivate(Guid? updatedBy = null)
    {
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void UpdateRange(DateOnly? rangeEnd, int? maxOccurrences, Guid? updatedBy = null)
    {
        RangeEnd       = rangeEnd;
        MaxOccurrences = maxOccurrences;
        UpdatedAt      = DateTimeOffset.UtcNow;
        UpdatedBy      = updatedBy;
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static readonly string[] ValidFrequencies = ["weekly", "biweekly", "monthly"];

    private static void ValidateFrequency(string frequency)
    {
        if (!ValidFrequencies.Contains(frequency))
            throw new ArgumentException(
                $"Invalid frequency '{frequency}'. Must be weekly, biweekly, or monthly.");
    }
}
