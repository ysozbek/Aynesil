namespace Aynesil.Domain.Events;

/// <summary>
/// Abstract base implementation of IDomainEvent.
/// Concrete domain events inherit from this class.
/// Example: public record StudentEnrolledEvent(Guid StudentId, Guid ProgramId) : BaseDomainEvent;
/// </summary>
public abstract record BaseDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
