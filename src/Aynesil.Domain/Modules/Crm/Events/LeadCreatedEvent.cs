using Aynesil.Domain.Events;

namespace Aynesil.Domain.Modules.Crm.Events;

/// <summary>Raised when a new lead is registered.</summary>
public record LeadCreatedEvent(
    Guid LeadId,
    Guid CorporationId,
    string ContactName,
    Guid? SourceId) : BaseDomainEvent;
