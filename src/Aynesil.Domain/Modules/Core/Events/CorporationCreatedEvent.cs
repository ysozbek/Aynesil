namespace Aynesil.Domain.Modules.Core.Events;

public record CorporationCreatedEvent(Guid CorporationId, string Code) : BaseDomainEvent;
