namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised when a new program is created.
/// Consumers: audit log, search index refresh.
/// </summary>
public record ProgramCreatedEvent(
    Guid ProgramId,
    Guid CorporationId,
    string Code,
    string Name) : BaseDomainEvent;
