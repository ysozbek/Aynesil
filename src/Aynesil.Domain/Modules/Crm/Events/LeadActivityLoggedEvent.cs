using Aynesil.Domain.Events;

namespace Aynesil.Domain.Modules.Crm.Events;

/// <summary>
/// Raised when a communication or activity is recorded against a lead.
/// When FollowUpAt is set, consumers can schedule a follow-up reminder.
/// </summary>
public record LeadActivityLoggedEvent(
    Guid ActivityId,
    Guid CorporationId,
    Guid LeadId,
    Guid? ActivityTypeId,
    DateTimeOffset? FollowUpAt) : BaseDomainEvent;
