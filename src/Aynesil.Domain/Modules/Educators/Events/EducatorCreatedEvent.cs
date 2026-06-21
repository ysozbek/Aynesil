namespace Aynesil.Domain.Modules.Educators.Events;

/// <summary>
/// Raised when a new educator record is created.
/// Consumers: audit log, notification service, HR onboarding workflow.
/// </summary>
public record EducatorCreatedEvent(
    Guid EducatorId,
    Guid CorporationId,
    string FirstName,
    string LastName,
    Guid? TitleId,
    Guid? CreatedBy) : BaseDomainEvent;
