namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised when a student is assigned to a specific program (StudentProgram created).
/// Consumers: audit log, notification service, scheduling module (ready for session planning).
/// </summary>
public record StudentAssignedToProgramEvent(
    Guid StudentProgramId,
    Guid CorporationId,
    Guid StudentId,
    Guid ProgramId,
    Guid? EnrollmentId,
    Guid? CampusId) : BaseDomainEvent;
