namespace Aynesil.Domain.Modules.Assessment.Events;

/// <summary>
/// Raised when an assessment report is finalized (locked as immutable).
/// Consumers: notification service (report ready for parent/guardian), audit log,
/// enrollment module (trigger program recommendation step).
/// </summary>
public record AssessmentReportFinalizedEvent(
    Guid ReportId,
    Guid CorporationId,
    Guid AssessmentSessionId,
    Guid FinalizedBy) : BaseDomainEvent;
