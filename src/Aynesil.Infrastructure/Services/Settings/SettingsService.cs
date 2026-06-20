using System.Text.Json;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Infrastructure.Persistence;
using Aynesil.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Services.Settings;

/// <summary>
/// Hierarchical settings resolution service.
/// Resolution order (most-specific-wins): user > campus > corporation > system default.
/// Results are cached per corporation. Invalidated on setting value changes.
/// </summary>
public interface ISettingsService
{
    Task<T?> GetAsync<T>(string key, Guid? corporationId = null, Guid? scopeId = null, string scopeLevel = "corporation", CancellationToken ct = default);
    Task SetAsync(string key, object value, Guid? corporationId, string scopeLevel, Guid? scopeId = null, Guid? updatedBy = null, CancellationToken ct = default);
}

public sealed class SettingsService : ISettingsService
{
    private readonly AynesilDbContext _db;
    private readonly ICacheService _cache;

    public SettingsService(AynesilDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        Guid? corporationId = null,
        Guid? scopeId = null,
        string scopeLevel = "corporation",
        CancellationToken ct = default)
    {
        var cacheKey = corporationId.HasValue
            ? CacheKeys.CorporationSettings(corporationId.Value) + $":{key}"
            : $"settings:system:{key}";

        return await _cache.GetOrSetAsync<T?>(cacheKey, async _ =>
        {
            // Walk the resolution chain: user → campus → corporation → system
            var scopes = new[]
            {
                scopeId.HasValue && scopeLevel == "user" ? ("user", corporationId, scopeId) : ((string?)null, (Guid?)null, (Guid?)null),
                scopeId.HasValue && scopeLevel == "campus" ? ("campus", corporationId, scopeId) : ((string?)null, (Guid?)null, (Guid?)null),
                corporationId.HasValue ? ("corporation", corporationId, (Guid?)null) : ((string?)null, (Guid?)null, (Guid?)null),
                ("system", (Guid?)null, (Guid?)null)
            };

            foreach (var (sl, cid, sid) in scopes)
            {
                if (sl is null) continue;

                var val = await _db.SettingValues
                    .AsNoTracking()
                    .Where(sv => sv.SettingKey == key
                        && sv.ScopeLevel == sl
                        && sv.CorporationId == cid
                        && sv.ScopeId == sid)
                    .Select(sv => sv.Value)
                    .FirstOrDefaultAsync(ct);

                if (val is not null)
                    return JsonSerializer.Deserialize<T>(val);
            }

            // Fall back to definition default
            var def = await _db.SettingDefinitions
                .AsNoTracking()
                .Where(d => d.Key == key)
                .Select(d => d.DefaultValue)
                .FirstOrDefaultAsync(ct);

            return def is not null ? JsonSerializer.Deserialize<T>(def) : default;
        }, TimeSpan.FromMinutes(15), ct);
    }

    public async Task SetAsync(
        string key,
        object value,
        Guid? corporationId,
        string scopeLevel,
        Guid? scopeId = null,
        Guid? updatedBy = null,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);

        var existing = await _db.SettingValues
            .FirstOrDefaultAsync(sv => sv.SettingKey == key
                && sv.ScopeLevel == scopeLevel
                && sv.CorporationId == corporationId
                && sv.ScopeId == scopeId, ct);

        if (existing is null)
        {
            _db.SettingValues.Add(new SettingValue
            {
                SettingKey = key,
                ScopeLevel = scopeLevel,
                CorporationId = corporationId,
                ScopeId = scopeId,
                Value = json,
                UpdatedBy = updatedBy,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            existing.Value = json;
            existing.UpdatedBy = updatedBy;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        // Invalidate cached settings for this corporation
        if (corporationId.HasValue)
            await _cache.RemoveByPrefixAsync(CacheKeys.CorporationSettings(corporationId.Value), ct);
    }
}
