using System.Collections.Concurrent;
using System.Text.Json;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared.Constants;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Aynesil.Infrastructure.Services.Cache;

/// <summary>
/// Redis-backed distributed cache with tenant-scoped key isolation.
/// Tenant keys are prefixed with corporation_id (via CacheKeys helper).
/// GetOrSetAsync implements cache-aside pattern.
/// RemoveByPrefixAsync uses Redis SCAN (with the same InstanceName prefix that
/// IDistributedCache applies) or an in-process key registry for memory cache.
/// Falls back gracefully on Redis unavailability — logs and returns default.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    /// <summary>
    /// Must match <c>AddStackExchangeRedisCache(... InstanceName)</c>.
    /// IDistributedCache stores keys as "{InstanceName}{logicalKey}"; SCAN must use the same prefix.
    /// </summary>
    internal const string InstanceName = "aynesil:";

    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Logical keys written through this service (no InstanceName). Used when Redis SCAN is unavailable.</summary>
    private static readonly ConcurrentDictionary<string, byte> KnownKeys = new();

    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null) return default;
            return JsonSerializer.Deserialize<T>(bytes, _json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache get failed for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _json);
            var opts = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
            };
            await _cache.SetAsync(key, bytes, opts, ct);
            KnownKeys.TryAdd(key, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache set failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
            KnownKeys.TryRemove(key, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache remove failed for key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        try
        {
            if (_redis is not null)
            {
                var server = _redis.GetServers().FirstOrDefault(s => s.IsConnected);
                if (server is null)
                {
                    _logger.LogWarning("Cannot remove by prefix — no connected Redis server.");
                    await RemoveKnownByPrefixAsync(prefix, ct);
                    return;
                }

                // IDistributedCache with InstanceName writes "aynesil:{logicalKey}".
                var redisKeys = server.Keys(pattern: $"{InstanceName}{prefix}*").ToArray();
                if (redisKeys.Length > 0)
                    await _redis.GetDatabase().KeyDeleteAsync(redisKeys);

                foreach (var logical in KnownKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
                    KnownKeys.TryRemove(logical, out _);

                _logger.LogDebug("Removed {Count} cache keys matching prefix {Prefix}", redisKeys.Length, prefix);
                return;
            }

            await RemoveKnownByPrefixAsync(prefix, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache remove-by-prefix failed for prefix {Prefix}", prefix);
        }
    }

    private async Task RemoveKnownByPrefixAsync(string prefix, CancellationToken ct)
    {
        var toRemove = KnownKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var key in toRemove)
        {
            await _cache.RemoveAsync(key, ct);
            KnownKeys.TryRemove(key, out _);
        }

        if (toRemove.Count == 0)
            _logger.LogWarning("No known cache keys matched prefix {Prefix}", prefix);
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory(ct);
        await SetAsync(key, value, expiry, ct);
        return value;
    }

    public async Task InvalidateTenantAsync(Guid corporationId, CancellationToken ct = default) =>
        await RemoveByPrefixAsync(CacheKeys.TenantPrefix(corporationId), ct);

    public async Task InvalidateMenuTreeAsync(Guid corporationId, CancellationToken ct = default)
    {
        // Explicit RemoveAsync goes through IDistributedCache and applies InstanceName correctly.
        foreach (var locale in CacheKeys.MenuLocales)
            await RemoveAsync(CacheKeys.MenuTree(corporationId, locale), ct);

        // Also clear any other locale variants via prefix (best-effort).
        await RemoveByPrefixAsync(CacheKeys.ForTenant(corporationId, "menu"), ct);
    }
}
