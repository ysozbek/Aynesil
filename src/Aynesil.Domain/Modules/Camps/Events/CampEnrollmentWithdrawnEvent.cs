namespace Aynesil.Domain.Modules.Camps.Events;

public record CampEnrollmentWithdrawnEvent(
    Guid EnrollmentId,
    Guid CampPeriodId,
    Guid StudentId,
    Guid CorporationId) : BaseDomainEvent;
