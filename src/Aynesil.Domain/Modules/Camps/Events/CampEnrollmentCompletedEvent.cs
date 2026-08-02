namespace Aynesil.Domain.Modules.Camps.Events;

public record CampEnrollmentCompletedEvent(
    Guid EnrollmentId,
    Guid CampPeriodId,
    Guid StudentId,
    Guid CorporationId) : BaseDomainEvent;
