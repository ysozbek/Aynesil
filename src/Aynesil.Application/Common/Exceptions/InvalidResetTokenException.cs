using Aynesil.Domain.Common;

namespace Aynesil.Application.Common.Exceptions;

/// <summary>
/// Parola sıfırlama token'ı geçersiz ya da süresi dolmuş (422, kod <see cref="MsgKeys.Auth.InvalidResetToken"/>).
/// 401 değildir: istemcinin oturum yenileme / login yönlendirme akışı tetiklenmemeli; sıfırlama ekranı
/// kodu görüp "yeni bağlantı iste" gösterir.
/// </summary>
public sealed class InvalidResetTokenException : Exception, ILocalizableException
{
    public InvalidResetTokenException() : base(MsgKeys.Auth.InvalidResetToken.Value) { }

    public MsgKey? Code => MsgKeys.Auth.InvalidResetToken;

    public IReadOnlyList<object?> Args => [];
}
