namespace Aynesil.Domain.Modules.Students.Events;

/// <summary>
/// Raised when a case note is added for a student.
/// Confidential notes should only be forwarded to consumers with the clinical-access permission.
/// Consumers: audit log, notification service (supervisor alert for confidential notes).
/// </summary>
public record CaseNoteAddedEvent(
    Guid CaseNoteId,
    Guid StudentId,
    Guid CorporationId,
    bool IsConfidential,
    Guid? AuthoredBy) : BaseDomainEvent;
