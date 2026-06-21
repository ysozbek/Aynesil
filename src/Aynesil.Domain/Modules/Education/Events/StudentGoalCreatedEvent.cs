namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised when a new individualized goal is created for a student.
/// Consumers: audit log, reporting.
/// </summary>
public record StudentGoalCreatedEvent(
    Guid StudentGoalId,
    Guid CorporationId,
    Guid StudentId,
    string Horizon,
    Guid? TemplateId,
    Guid? CreatedBy) : BaseDomainEvent;
