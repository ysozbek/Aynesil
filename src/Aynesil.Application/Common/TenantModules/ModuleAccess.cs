using System.Text.Json;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Common.TenantModules;

/// <summary>`settings.modules` ayrıştırma ve koruma yardımcıları.</summary>
public static class CorporationModules
{
    public const string Key = "modules";

    /// <summary>Settings JSON'undan etkin modül durumu; eksik/bozuk anahtar = açık.</summary>
    public static IReadOnlyDictionary<string, bool> Parse(string? settingsJson)
    {
        var result = Modules.All.ToDictionary(m => m, _ => true);
        if (string.IsNullOrWhiteSpace(settingsJson)) return result;
        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty(Key, out var mods)
                || mods.ValueKind != JsonValueKind.Object)
                return result;

            foreach (var m in Modules.All)
                if (mods.TryGetProperty(m, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    result[m] = v.GetBoolean();
        }
        catch (JsonException) { /* bozuk settings: hepsi açık */ }
        return result;
    }

    /// <summary>
    /// Kurum yöneticisinin gönderdiği settings'ten `modules` anahtarını atar ve mevcut (platformun
    /// yazdığı) `modules` değerini korur. Gönderilen `modules` sessizce yok sayılır.
    /// </summary>
    public static string PreserveModules(string incomingSettingsJson, string? currentSettingsJson)
    {
        JsonElement? current = null;
        JsonDocument? currentDoc = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(currentSettingsJson))
            {
                currentDoc = JsonDocument.Parse(currentSettingsJson);
                if (currentDoc.RootElement.ValueKind == JsonValueKind.Object
                    && currentDoc.RootElement.TryGetProperty(Key, out var c))
                    current = c;
            }
        }
        catch (JsonException) { /* bozuk mevcut değer: korunacak bir şey yok */ }

        try
        {
            using var incoming = JsonDocument.Parse(incomingSettingsJson);
            using var ms = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms))
            {
                w.WriteStartObject();
                foreach (var p in incoming.RootElement.EnumerateObject())
                    if (!string.Equals(p.Name, Key, StringComparison.OrdinalIgnoreCase))
                        p.WriteTo(w);
                if (current.HasValue)
                {
                    w.WritePropertyName(Key);
                    current.Value.WriteTo(w);
                }
                w.WriteEndObject();
            }
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        finally { currentDoc?.Dispose(); }
    }
}

/// <summary>İstek başına tek sorgu; çapraz istek önbelleği yok (devops/set_modules.py değişikliği hemen etkili olsun).</summary>
public sealed class ModuleAccess : IModuleAccess
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private IReadOnlyDictionary<string, bool>? _cache;

    public ModuleAccess(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<bool> IsEnabledAsync(string module, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return !all.TryGetValue(module, out var enabled) || enabled;
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache is not null) return _cache;

        var corporationId = _tenant.CorporationId;
        string? settings = null;
        if (corporationId.HasValue)
            settings = await _db.Corporations.AsNoTracking()
                .Where(c => c.Id == corporationId.Value)
                .Select(c => c.Settings)
                .FirstOrDefaultAsync(ct);

        return _cache = CorporationModules.Parse(settings);
    }
}
