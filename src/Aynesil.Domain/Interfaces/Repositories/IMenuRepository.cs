using Aynesil.Domain.Modules.Iam.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Typed repository for <see cref="MenuItem"/> aggregate root.
/// Provides specialised queries needed by the menu module that benefit
/// from a typed contract over raw IAppDbContext usage in handlers.
/// </summary>
public interface IMenuRepository : IRepository<MenuItem>
{
    /// <summary>
    /// Returns all active menu items visible to a corporation:
    /// platform defaults (corporation_id = NULL) plus tenant-scoped items.
    /// Eagerly loads <see cref="MenuItem.Translations"/> and
    /// <see cref="MenuItem.RequiredPermission"/> for in-memory filtering.
    /// </summary>
    Task<IReadOnlyList<MenuItem>> GetActiveFlatListAsync(
        Guid corporationId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single menu item with its <see cref="MenuItem.Translations"/>
    /// and <see cref="MenuItem.RequiredPermission"/> navigation properties loaded.
    /// Returns null when not found.
    /// </summary>
    Task<MenuItem?> GetWithTranslationsAsync(
        Guid id,
        CancellationToken ct = default);
}
