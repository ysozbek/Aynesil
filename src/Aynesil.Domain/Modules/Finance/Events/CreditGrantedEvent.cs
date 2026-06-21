namespace Aynesil.Domain.Modules.Finance.Events;

/// <summary>
/// Raised when credits are granted to a student package (initial purchase or bonus).
/// </summary>
public record CreditGrantedEvent(
    Guid LedgerEntryId,
    Guid CorporationId,
    Guid StudentPackageId,
    decimal Amount,
    string Reason) : BaseDomainEvent;
