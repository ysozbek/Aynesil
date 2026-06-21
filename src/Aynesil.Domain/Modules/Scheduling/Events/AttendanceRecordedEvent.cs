namespace Aynesil.Domain.Modules.Scheduling.Events;

/// <summary>
/// Raised when attendance is recorded for a student in a session.
/// </summary>
public record AttendanceRecordedEvent(
    Guid AttendanceId,
    Guid CorporationId,
    Guid SessionId,
    Guid StudentId,
    string Status) : BaseDomainEvent;
