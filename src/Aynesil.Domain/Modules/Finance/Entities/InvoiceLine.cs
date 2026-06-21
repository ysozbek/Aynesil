namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// A single line item on an invoice, representing one package or service charge.
/// Lines are created with the invoice and cascade-deleted when the invoice is soft-deleted.
/// No independent audit or soft-delete — follows the parent invoice lifecycle.
///
/// Maps to finance.invoice_line.
/// </summary>
public class InvoiceLine : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid? StudentPackageId { get; private set; }
    public decimal Quantity { get; private set; } = 1;
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
    public int SortOrder { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static InvoiceLine Create(
        Guid corporationId,
        Guid invoiceId,
        string description,
        decimal unitPrice,
        decimal quantity = 1,
        Guid? studentPackageId = null,
        int sortOrder = 0)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.");

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.");

        return new InvoiceLine
        {
            CorporationId    = corporationId,
            InvoiceId        = invoiceId,
            Description      = description,
            StudentPackageId = studentPackageId,
            Quantity         = quantity,
            UnitPrice        = unitPrice,
            LineTotal        = Math.Round(quantity * unitPrice, 2),
            SortOrder        = sortOrder
        };
    }
}
