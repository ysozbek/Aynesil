namespace Aynesil.Domain.Modules.Students.Events;

/// <summary>
/// Raised when a new student record is created (either directly or via lead conversion).
/// Consumers: audit log, notification service (welcome workflow), CRM module (update lead state).
/// </summary>
public record StudentCreatedEvent(
    Guid StudentId,
    Guid CorporationId,
    string FirstName,
    string LastName,
    Guid? LeadId,
    Guid? CreatedBy) : BaseDomainEvent;
