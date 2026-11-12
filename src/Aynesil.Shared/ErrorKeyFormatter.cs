namespace Aynesil.Shared;

/// <summary>
/// Hata anahtarlarını JSON camelCase biçimine çevirir; iç içe yolları segment segment işler.
/// "Lines[0].Amount" → "lines[0].amount", "Code" → "code".
/// </summary>
public static class ErrorKeyFormatter
{
    public static string ToCamelCase(string key) =>
        string.Join('.', key.Split('.').Select(s =>
            s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..]));

    /// <summary>Anahtarları camelCase yapar; çakışan anahtarların mesajlarını birleştirir.</summary>
    public static Dictionary<string, string[]> Normalize(IEnumerable<KeyValuePair<string, string[]>> errors) =>
        errors
            .GroupBy(kv => ToCamelCase(kv.Key))
            .ToDictionary(g => g.Key, g => g.SelectMany(kv => kv.Value).Distinct().ToArray());

    /// <summary>Kodlu hata ayrıntıları için <see cref="Normalize(IEnumerable{KeyValuePair{string, string[]}})"/> karşılığı.</summary>
    public static Dictionary<string, ErrorDetail[]> Normalize(IEnumerable<KeyValuePair<string, ErrorDetail[]>> errors) =>
        errors
            .GroupBy(kv => ToCamelCase(kv.Key))
            .ToDictionary(g => g.Key, g => g.SelectMany(kv => kv.Value).Distinct().ToArray());
}
