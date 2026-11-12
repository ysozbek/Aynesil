using Aynesil.Domain.Common;

namespace Aynesil.Application.Common.Exceptions;

/// <summary>
/// Giriş başarısız (401). Kod + parametre taşır; metin istek dilinde middleware'de çözülür.
/// Kodlar: <see cref="MsgKeys.Auth.InvalidCredentials"/>, <see cref="MsgKeys.Auth.AccountLocked"/>,
/// <see cref="MsgKeys.Auth.AccountDisabled"/>. Hesap durumu (kilitli/devre dışı) kullanıcı varlığını sızdırmamak için
/// yalnızca parola doğru girildiğinde bildirilir.
/// </summary>
public sealed class AuthenticationFailedException : Exception, ILocalizableException
{
    public AuthenticationFailedException(MsgKey code, params object?[] args) : base(code.Value)
    {
        Code = code;
        Args = args;
    }

    public MsgKey? Code { get; }

    public IReadOnlyList<object?> Args { get; }
}
