namespace Aynesil.Domain.Modules.Finance.Events;

/// <summary>
/// Raised when a refund is successfully processed.
/// Downstream handlers may update the related payment status and notify the guardian.
/// </summary>
public record RefundProcessedEvent(
    Guid RefundId,
    Guid CorporationId,
    Guid PaymentId,
    decimal Amount) : BaseDomainEvent;
