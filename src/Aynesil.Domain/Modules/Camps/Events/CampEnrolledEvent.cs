namespace Aynesil.Domain.Modules.Camps.Events;

public record CampEnrolledEvent(
    Guid EnrollmentId,
    Guid CampPeriodId,
    Guid StudentId,
    Guid CorporationId,
    string Status) : BaseDomainEvent;
