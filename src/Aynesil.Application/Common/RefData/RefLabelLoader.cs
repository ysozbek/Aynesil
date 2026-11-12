using Aynesil.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Common.RefData;

/// <summary>
/// Modüller arası paylaşılan ref_value etiket çözümleyici.
/// Aktif/pasif ayrımı yapmaz: pasifleştirilmiş değerler de geçmiş kayıtlarda çözülür.
/// </summary>
public static class RefLabelLoader
{
    /// <summary>
    /// ref_value adları: kullanıcı dili → 'tr' → herhangi bir çeviri → kod.
    /// Bulunamayan kimlik sözlükte yer almaz.
    /// </summary>
    public static async Task<Dictionary<Guid, string>> LoadAsync(
        IAppDbContext db, IReadOnlyCollection<Guid> ids, string? locale, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        var codes = await db.RefValues.AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .Select(v => new { v.Id, v.Code })
            .ToDictionaryAsync(v => v.Id, v => v.Code, ct);

        var translations = (await db.RefValueTranslations.AsNoTracking()
                .Where(t => ids.Contains(t.RefValueId))
                .Select(t => new { t.RefValueId, t.Locale, t.Label })
                .ToListAsync(ct))
            .GroupBy(t => t.RefValueId)
            .ToDictionary(
                g => g.Key,
                g => (g.FirstOrDefault(t => t.Locale == locale)
                      ?? g.FirstOrDefault(t => t.Locale == "tr")
                      ?? g.First()).Label);

        return codes.ToDictionary(c => c.Key, c => translations.GetValueOrDefault(c.Key) ?? c.Value);
    }
}
