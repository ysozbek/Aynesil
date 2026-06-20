using Aynesil.Domain.Events;

namespace Aynesil.Domain.Modules.Crm.Events;

/// <summary>Raised when a pre-enrollment interview is scheduled for a lead.</summary>
public record InterviewScheduledEvent(
    Guid InterviewId,
    Guid CorporationId,
    Guid LeadId,
    DateTimeOffset? ScheduledAt) : BaseDomainEvent;
