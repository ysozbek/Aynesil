namespace Aynesil.Domain.Common;

/// <summary>
/// Yerelleştirilebilir mesaj anahtarı (ör. <c>Error.NotFound</c>). Düz string yerine ayrı bir tip olması,
/// istisnalara ham metin yerine yanlışlıkla kod geçirilmesini (veya tersini) derleme zamanında ayırır.
/// Anahtar aynı zamanda API yanıtındaki makine okunur hata kodudur.
/// </summary>
public readonly record struct MsgKey(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// Kod + parametre taşıyan istisna. Metin istisnada değil, middleware'de istek diline göre çözülür.
/// <see cref="Args"/> öğeleri düz değer veya ertelenmiş etiket (<c>ILocalizedArg</c>) olabilir.
/// </summary>
public interface ILocalizableException
{
    MsgKey? Code { get; }
    IReadOnlyList<object?> Args { get; }
}

/// <summary>Mesaj parametresi: kod değerinin (durum, eylem, birim…) istek dilindeki etiketi (EnumLabels.resx).</summary>
public sealed record EnumValue(string Group, string Code);

/// <summary>Mesaj parametresi: sayı/tarih değeri, biçimlendirme anında istek kültürüyle (ör. "F2") yazılır.</summary>
public sealed record Formatted(object? Value, string Format);

/// <summary>Mesaj parametresi: alan adının istek dilindeki etiketi (FieldNames.resx; property/camel/snake adı kabul eder).</summary>
public sealed record FieldRef(string Name);

/// <summary>Mesaj parametresi: alan adı listesi, seçili dilde etiketlerle ("Puan, Yorum").</summary>
public sealed record FieldRefs(string[] Names);

/// <summary>Mesaj parametresi: kod listesi, seçili dilde etiketlerle ("devam, geç").</summary>
public sealed record EnumValues(string Group, IReadOnlyList<string> Codes);

/// <summary>Domain katmanında mesaj parametresi üretmek için kısa yollar.</summary>
public static class MsgArg
{
    public static FieldRef Field(string name) => new(name);
    public static FieldRefs Fields(params string[] names) => new(names);
    public static EnumValues Values(string group, params string[] codes) => new(group, codes);
    public static EnumValues Values(string group, IEnumerable<string> codes) => new(group, codes.ToList());
}

/// <summary>
/// Domain guard'larının (tarih sırası, tutar, zorunlu alan, geçersiz kod değeri…) attığı, kod + parametre taşıyan hata.
/// Middleware 422 döner; metin istek dilinde çözülür.
/// </summary>
public class InvalidArgumentException : ArgumentException, ILocalizableException
{
    public InvalidArgumentException(MsgKey code, params object?[] args) : base(code.Value)
    {
        Code = code;
        Args = args;
    }

    public MsgKey? Code { get; }

    public IReadOnlyList<object?> Args { get; }
}
