namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Redis-backed distributed cache abstraction.
/// Tenant-scoped keys are prefixed with corporation_id to prevent cross-tenant reads.
/// Cache entries targeted for invalidation:
///   - Permissions     (on role/permission change)
///   - Menus           (on menu item change)
///   - Settings        (on setting value change)
///   - Reference Data  (on ref_value change)
///   - Translations    (on i18n_message change)
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    /// <summary>Invalidates all cache entries for a specific corporation.</summary>
    Task InvalidateTenantAsync(Guid corporationId, CancellationToken cancellationToken = default);
}
