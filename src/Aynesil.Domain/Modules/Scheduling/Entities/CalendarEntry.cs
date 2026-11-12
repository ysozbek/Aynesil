using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// A school-wide or campus-specific calendar event: holiday, closure, event, or term break.
/// Used for conflict detection (sessions should not be scheduled on closure days).
/// campus_id == null means the entry applies to the whole corporation.
///
/// Maps to scheduling.calendar_entry.
/// Minimal audit: created_at only.
/// </summary>
public class CalendarEntry : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid? CampusId { get; private set; }
    public string Title { get; private set; } = string.Empty;

    /// <summary>holiday | closure | event | term_break</summary>
    public string EntryType { get; private set; } = "holiday";

    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt   { get; private set; }
    public bool IsAllDay           { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static CalendarEntry Create(
        Guid corporationId,
        string title,
        string entryType,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        bool isAllDay = true,
        Guid? campusId = null)
    {
        ValidateEntryType(entryType);

        if (endsAt <= startsAt)
            throw new InvalidArgumentException(MsgKeys.Validation.MustBeAfter, MsgArg.Field("EndsAt"), MsgArg.Field("StartsAt"));

        return new CalendarEntry
        {
            CorporationId = corporationId,
            CampusId      = campusId,
            Title         = title,
            EntryType     = entryType,
            StartsAt      = startsAt,
            EndsAt        = endsAt,
            IsAllDay      = isAllDay,
            CreatedAt     = DateTimeOffset.UtcNow
        };
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static readonly string[] ValidTypes = ["holiday", "closure", "event", "term_break"];

    private static void ValidateEntryType(string type)
    {
        if (!ValidTypes.Contains(type))
            throw new InvalidArgumentException(MsgKeys.Validation.MustBeOneOf, MsgArg.Field("EntryType"), MsgArg.Values("EntryType", "holiday", "closure", "event", "term_break"));
    }
}
