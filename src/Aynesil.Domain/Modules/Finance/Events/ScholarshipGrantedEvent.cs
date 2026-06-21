namespace Aynesil.Domain.Modules.Finance.Events;

/// <summary>
/// Raised when a scholarship is granted to a student.
/// Downstream handlers may notify the student/guardian or re-calculate outstanding balances.
/// </summary>
public record ScholarshipGrantedEvent(
    Guid ScholarshipId,
    Guid CorporationId,
    Guid StudentId,
    decimal? Percentage,
    decimal? Amount,
    Guid? ApprovedBy) : BaseDomainEvent;
