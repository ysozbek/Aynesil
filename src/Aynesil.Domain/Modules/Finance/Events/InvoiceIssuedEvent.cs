namespace Aynesil.Domain.Modules.Finance.Events;

/// <summary>
/// Raised when an invoice transitions from draft to issued.
/// Downstream handlers may trigger payment-due notifications.
/// </summary>
public record InvoiceIssuedEvent(
    Guid InvoiceId,
    Guid CorporationId,
    Guid? StudentId,
    decimal GrandTotal,
    string Currency) : BaseDomainEvent;
