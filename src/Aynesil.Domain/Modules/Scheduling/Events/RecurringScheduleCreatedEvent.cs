namespace Aynesil.Domain.Modules.Scheduling.Events;

/// <summary>
/// Raised when a recurring schedule rule is created.
/// The materialisation job will pick this up and generate concrete session rows.
/// </summary>
public record RecurringScheduleCreatedEvent(
    Guid RecurringScheduleId,
    Guid CorporationId,
    string Frequency,
    DateOnly RangeStart,
    DateOnly? RangeEnd,
    Guid? CreatedBy) : BaseDomainEvent;
