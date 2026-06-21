using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// An override for a specific occurrence date of a recurring schedule.
/// Actions: skip (omit), reschedule (move to new_start_at), cancel.
///
/// Maps to scheduling.recurrence_exception.
/// No audit fields in DDL — append-only, cascade-deleted with the parent schedule.
/// </summary>
public class RecurrenceException : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid RecurringScheduleId { get; private set; }
    public DateOnly ExceptionDate { get; private set; }

    /// <summary>skip | reschedule | cancel</summary>
    public string Action { get; private set; } = "skip";

    /// <summary>New start time when action = 'reschedule'.</summary>
    public DateTimeOffset? NewStartAt { get; private set; }

    public string? Reason { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static RecurrenceException Create(
        Guid corporationId,
        Guid recurringScheduleId,
        DateOnly exceptionDate,
        string action,
        DateTimeOffset? newStartAt = null,
        string? reason = null)
    {
        ValidateAction(action);

        if (action == "reschedule" && newStartAt is null)
            throw new ArgumentException("new_start_at is required when action is 'reschedule'.");

        return new RecurrenceException
        {
            CorporationId        = corporationId,
            RecurringScheduleId  = recurringScheduleId,
            ExceptionDate        = exceptionDate,
            Action               = action,
            NewStartAt           = newStartAt,
            Reason               = reason
        };
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static readonly string[] ValidActions = ["skip", "reschedule", "cancel"];

    private static void ValidateAction(string action)
    {
        if (!ValidActions.Contains(action))
            throw new ArgumentException(
                $"Invalid recurrence exception action '{action}'. Must be skip, reschedule, or cancel.");
    }
}
