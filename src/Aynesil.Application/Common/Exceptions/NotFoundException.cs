using Aynesil.Domain.Common;

namespace Aynesil.Application.Common.Exceptions;

/// <summary>
/// Kayıt bulunamadı (404). <see cref="KeyNotFoundException"/>'dan türer: eski catch/test beklentileriyle uyumludur.
/// Kullanım: <c>new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Student"))</c>;
/// metin ve kod middleware'de istek diline göre çözülür.
/// </summary>
public class NotFoundException : KeyNotFoundException, ILocalizableException
{
    public NotFoundException(MsgKey code, params object?[] args) : base(code.Value)
    {
        Code = code;
        Args = args;
    }

    public MsgKey? Code { get; }

    public IReadOnlyList<object?> Args { get; }
}
