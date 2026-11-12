using System.Globalization;

namespace Aynesil.Application.Common.Localization;

/// <summary>
/// İstek dilini çözer. Öncelik: Accept-Language (arayüz seçimi) → kullanıcı tercihi (JWT locale) → tr.
/// Yalnızca desteklenen diller (tr, en) kabul edilir; q-değerleri dikkate alınır.
/// </summary>
public static class LocaleResolver
{
    public const string Default = "tr";

    public static readonly IReadOnlyList<string> Supported = ["tr", "en"];

    /// <summary>"en-US" → "en"; desteklenmeyen veya boş değer için null.</summary>
    public static string? Normalize(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        var primary = tag.Trim().Split('-', '_')[0].ToLowerInvariant();
        return Supported.Contains(primary) ? primary : null;
    }

    /// <summary>Accept-Language başlığındaki en yüksek öncelikli desteklenen dil (yoksa null).</summary>
    public static string? FromAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header)) return null;

        var candidates = new List<(string Locale, double Q, int Order)>();
        var order = 0;
        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segments = part.Split(';', StringSplitOptions.TrimEntries);
            var q = 1.0;
            foreach (var s in segments.Skip(1))
            {
                if (s.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(s.AsSpan(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                    q = parsed;
            }

            var locale = Normalize(segments[0]);
            if (locale is not null && q > 0)
                candidates.Add((locale, q, order++));
        }

        return candidates.Count == 0
            ? null
            : candidates.OrderByDescending(c => c.Q).ThenBy(c => c.Order).First().Locale;
    }

    public static string Resolve(string? acceptLanguage, string? userLocale) =>
        FromAcceptLanguage(acceptLanguage) ?? Normalize(userLocale) ?? Default;
}
