namespace Aynesil.Domain.Modules.Legal.Events;

/// <summary>
/// Raised when a guardian/student grants a consent.
/// Used by downstream handlers such as camera-viewing authorization setup
/// (media module) and KVKK audit logging.
/// </summary>
public record ConsentGrantedEvent(
    Guid ConsentId,
    Guid CorporationId,
    Guid StudentId,
    Guid? ConsentTypeId) : BaseDomainEvent;
