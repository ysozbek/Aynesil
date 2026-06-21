namespace Aynesil.Domain.Modules.Finance.Events;

/// <summary>
/// Raised when a student package is purchased.
/// Downstream handlers can trigger invoice generation, credit grant, or notifications.
/// </summary>
public record PackagePurchasedEvent(
    Guid StudentPackageId,
    Guid CorporationId,
    Guid StudentId,
    decimal TotalCredits,
    decimal Price,
    Guid? PackageDefinitionId,
    Guid? PurchasedBy) : BaseDomainEvent;
