namespace Aynesil.Domain.Modules.Camps.Events;

public record CampAttendanceRecordedEvent(
    Guid AttendanceId,
    Guid EnrollmentId,
    Guid CorporationId,
    DateOnly AttendanceDate,
    string Status) : BaseDomainEvent;
