namespace Aynesil.Domain.Modules.Students.Events;

/// <summary>
/// Raised when a student is enrolled at a campus (new StudentCampus record created).
/// Consumers: audit log, notification service, campus capacity reporting.
/// </summary>
public record StudentEnrolledEvent(
    Guid StudentId,
    Guid CorporationId,
    Guid CampusId,
    bool IsPrimary,
    Guid? EnrolledBy) : BaseDomainEvent;
