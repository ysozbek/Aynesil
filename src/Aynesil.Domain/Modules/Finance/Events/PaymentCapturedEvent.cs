namespace Aynesil.Domain.Modules.Finance.Events;

/// <summary>
/// Raised when a payment is successfully captured.
/// Downstream handlers may update invoice status to paid/partial and trigger receipt generation.
/// </summary>
public record PaymentCapturedEvent(
    Guid PaymentId,
    Guid CorporationId,
    Guid? InvoiceId,
    Guid? StudentId,
    decimal Amount,
    string Currency) : BaseDomainEvent;
