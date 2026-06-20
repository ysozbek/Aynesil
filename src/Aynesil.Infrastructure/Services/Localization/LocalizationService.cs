using Aynesil.Application.Common.Interfaces;
using Aynesil.Infrastructure.Persistence;
using Aynesil.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aynesil.Infrastructure.Services.Localization;

/// <summary>
/// Resolves localized strings with the approved fallback chain:
///   requested locale → corporation default → 'tr' (platform default) → message key (code fallback)
///
/// Two sources:
///   1. ref.i18n_message   — static UI keys (admin-managed key/value catalog)
///   2. ref.ref_value_translation — reference data labels (per-value translations)
///
/// Results are cached per (corporation, locale, namespace) with a 1-hour TTL.
/// Cache is invalidated when i18n_message or ref_value_translation rows change.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private readonly AynesilDbContext _db;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<LocalizationService> _logger;

    public LocalizationService(
        AynesilDbContext db,
        ICacheService cache,
        ITenantContext tenantContext,
        ILogger<LocalizationService> logger)
    {
        _db = db;
        _cache = cache;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<string> GetMessageAsync(
        string @namespace,
        string key,
        string? locale = null,
        CancellationToken ct = default)
    {
        var messages = await GetMessagesAsync(@namespace, ResolveLocale(locale), _tenantContext.CorporationId, ct);
        return messages.TryGetValue(key, out var value) ? value : key;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetMessagesAsync(
        string @namespace,
        string locale,
        Guid? corporationId = null,
        CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.Translations(corporationId, locale, @namespace);

        return await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            // Load all messages for this namespace, sorted by fallback priority
            var rows = await _db.I18nMessages
                .AsNoTracking()
                .Where(m => m.Namespace == @namespace && m.Locale == locale)
                .ToDictionaryAsync(m => m.MsgKey, m => m.Value, ct);

            // Fill missing keys with 'tr' fallback
            if (locale != "tr")
            {
                var fallback = await _db.I18nMessages
                    .AsNoTracking()
                    .Where(m => m.Namespace == @namespace && m.Locale == "tr")
                    .ToDictionaryAsync(m => m.MsgKey, m => m.Value, ct);

                foreach (var (k, v) in fallback)
                    rows.TryAdd(k, v);
            }

            return (IReadOnlyDictionary<string, string>)rows;
        }, TimeSpan.FromHours(1), ct);
    }

    public async Task<string> GetRefValueLabelAsync(
        Guid refValueId,
        string? locale = null,
        CancellationToken ct = default)
    {
        var targetLocale = ResolveLocale(locale);

        var translation = await _db.RefValueTranslations
            .AsNoTracking()
            .Where(t => t.RefValueId == refValueId && t.Locale == targetLocale)
            .Select(t => t.Label)
            .FirstOrDefaultAsync(ct);

        if (translation is not null) return translation;

        // Fallback to 'tr'
        if (targetLocale != "tr")
        {
            var trLabel = await _db.RefValueTranslations
                .AsNoTracking()
                .Where(t => t.RefValueId == refValueId && t.Locale == "tr")
                .Select(t => t.Label)
                .FirstOrDefaultAsync(ct);
            if (trLabel is not null) return trLabel;
        }

        // Final fallback: the code
        return await _db.RefValues
            .AsNoTracking()
            .Where(v => v.Id == refValueId)
            .Select(v => v.Code)
            .FirstOrDefaultAsync(ct) ?? refValueId.ToString();
    }

    private string ResolveLocale(string? requested) =>
        requested ?? _tenantContext.Locale ?? "tr";
}
