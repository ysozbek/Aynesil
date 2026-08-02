namespace Aynesil.Domain.Modules.Camps.Events;

public record CampActivityParticipationRecordedEvent(
    Guid ParticipationId,
    Guid CorporationId,
    Guid CampActivityId,
    Guid CampEnrollmentId,
    string Status) : BaseDomainEvent;
