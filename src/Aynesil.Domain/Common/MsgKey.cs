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
