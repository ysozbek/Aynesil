namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised when a student is enrolled (Enrollment record created).
/// Note: lives in the Education bounded context — distinct from the students module's
/// StudentEnrolledEvent (which tracks campus enrollment rather than program enrollment).
/// Consumers: audit log, notification service, reporting.
/// </summary>
public record StudentEnrolledEvent(
    Guid EnrollmentId,
    Guid CorporationId,
    Guid StudentId,
    Guid? CampusId,
    Guid? StatusId,
    Guid? CreatedBy) : BaseDomainEvent;
