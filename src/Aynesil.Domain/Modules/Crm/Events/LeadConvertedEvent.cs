using Aynesil.Domain.Events;

namespace Aynesil.Domain.Modules.Crm.Events;

/// <summary>
/// Raised when a lead is successfully converted to a student.
/// Other modules (e.g. Students, Notifications) can subscribe to initiate onboarding.
/// </summary>
public record LeadConvertedEvent(
    Guid LeadId,
    Guid CorporationId,
    Guid StudentId,
    Guid? ConvertedBy) : BaseDomainEvent;
