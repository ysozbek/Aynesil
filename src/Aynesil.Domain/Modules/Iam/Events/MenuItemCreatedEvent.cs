namespace Aynesil.Domain.Modules.Iam.Events;

/// <summary>
/// Raised when a new menu item is created (either a platform default or a tenant-scoped custom item).
/// </summary>
public sealed record MenuItemCreatedEvent(
    Guid MenuItemId,
    Guid? CorporationId,
    string Code) : BaseDomainEvent;
