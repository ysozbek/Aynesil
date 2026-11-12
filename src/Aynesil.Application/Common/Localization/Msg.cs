using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace Aynesil.Application.Common.Localization;

/// <summary>Biçimlendirme anına kadar ertelenen (kültüre bağlı) mesaj parametresi.</summary>
public interface ILocalizedArg
{
    string Resolve(CultureInfo culture);
}

/// <summary>
/// .resx kaynaklarından (Messages, FieldNames, EnumLabels) mesaj çözümü. Kültür varsayılan olarak
/// <see cref="CultureInfo.CurrentUICulture"/>'dur (istek başına RequestCulture middleware'i ayarlar).
/// Nötr kaynak tr'dir. Bulunamayan anahtar için anahtarın kendisi döner (testler bunu yakalar).
/// </summary>
public static partial class Msg
{
    private static readonly ResourceManager MessagesRm = new("Aynesil.Application.Resources.Messages", typeof(Msg).Assembly);
    private static readonly ResourceManager FieldsRm = new("Aynesil.Application.Resources.FieldNames", typeof(Msg).Assembly);
    private static readonly ResourceManager EnumsRm = new("Aynesil.Application.Resources.EnumLabels", typeof(Msg).Assembly);

    internal static ResourceManager Messages => MessagesRm;
    internal static ResourceManager Fields => FieldsRm;
    internal static ResourceManager Enums => EnumsRm;

    [GeneratedRegex(@"\{(\d+)\}")]
    private static partial Regex IndexPlaceholder();

    /// <summary>Anahtarı geçerli dilde çözer; <c>{0}</c>, <c>{1}</c> yer tutucularını args ile doldurur.</summary>
    public static string Get(string key, params object?[] args) => Get(key, CultureInfo.CurrentUICulture, args);

    public static string Get(string key, CultureInfo culture, params object?[] args)
    {
        var template = Lookup(MessagesRm, key, culture) ?? key;
        if (args.Length == 0) return template;

        // string.Format kullanılmaz: FluentValidation'ın {PropertyName} gibi adlı yer tutucuları korunmalı.
        return IndexPlaceholder().Replace(template, m =>
        {
            var i = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            if (i >= args.Length) return m.Value;
            return args[i] switch
            {
                ILocalizedArg a => a.Resolve(culture),
                IFormattable f => f.ToString(null, culture),
                var o => o?.ToString() ?? string.Empty,
            };
        });
    }

    /// <summary>Alan adı etiketi (property/camel/snake adı kabul eder); bulunamazsa okunur hale getirir.</summary>
    public static string Field(string name) => Field(name, CultureInfo.CurrentUICulture);

    public static string Field(string name, CultureInfo culture)
    {
        var key = ToPascal(name);
        if (Lookup(FieldsRm, key, culture) is { } known) return known;

        var parts = Regex.Split(key, @"(?<=[a-z0-9])(?=[A-Z])")
            .Where(p => p.Length > 0)
            .Select(p => Lookup(FieldsRm, "Word." + p, culture) ?? p)
            .ToArray();
        return parts.Length == 0 ? name : string.Join(' ', parts);
    }

    /// <summary>Kod değeri etiketi (örn. group "Outcome", code "on_track"); bulunamazsa ham kod.</summary>
    public static string Enum(string group, string code) => Enum(group, code, CultureInfo.CurrentUICulture);

    public static string Enum(string group, string code, CultureInfo culture) =>
        Lookup(EnumsRm, $"{group}.{code}", culture) ?? code;

    /// <summary>Mesaj parametresi olarak kullanılacak ertelenmiş alan adı.</summary>
    public static ILocalizedArg FieldArg(string name) => new Deferred(c => Field(name, c));

    /// <summary>Mesaj parametresi: kod listesi, seçili dilde etiketlerle ("devam, geç").</summary>
    public static ILocalizedArg EnumList(string group, params string[] codes) =>
        new Deferred(c => string.Join(", ", codes.Select(code => Enum(group, code, c))));

    /// <summary>Mesaj parametresi: tek kod değerinin etiketi.</summary>
    public static ILocalizedArg EnumArg(string group, string code) => new Deferred(c => Enum(group, code, c));

    private sealed record Deferred(Func<CultureInfo, string> Factory) : ILocalizedArg
    {
        public string Resolve(CultureInfo culture) => Factory(culture);
    }

    private static string? Lookup(ResourceManager rm, string key, CultureInfo culture)
    {
        try { return rm.GetString(key, culture); }
        catch (MissingManifestResourceException) { return null; }
    }

    /// <summary>"new_start_at" / "periodStart" / "PeriodStart" → "NewStartAt" / "PeriodStart".</summary>
    internal static string ToPascal(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new StringBuilder(name.Length);
        var upperNext = true;
        foreach (var ch in name)
        {
            if (ch is '_' or '-' or ' ') { upperNext = true; continue; }
            sb.Append(upperNext ? char.ToUpperInvariant(ch) : ch);
            upperNext = false;
        }
        return sb.ToString();
    }
}
