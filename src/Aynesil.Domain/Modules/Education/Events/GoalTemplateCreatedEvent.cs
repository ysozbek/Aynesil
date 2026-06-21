namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised when a new goal template is created (platform or tenant-level).
/// Consumers: audit log, search index.
/// </summary>
public record GoalTemplateCreatedEvent(
    Guid GoalTemplateId,
    Guid? CorporationId,
    string Statement,
    Guid? LibraryId,
    Guid? CategoryId) : BaseDomainEvent;
