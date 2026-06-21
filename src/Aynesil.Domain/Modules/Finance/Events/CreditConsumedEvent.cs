namespace Aynesil.Domain.Modules.Finance.Events;

/// <summary>
/// Raised when credits are consumed from a student package — typically on session completion.
/// Downstream handlers may check for low-credit or exhausted state and alert guardians.
/// </summary>
public record CreditConsumedEvent(
    Guid LedgerEntryId,
    Guid CorporationId,
    Guid StudentPackageId,
    decimal Amount,
    Guid? SessionId) : BaseDomainEvent;
