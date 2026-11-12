using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using FluentValidation.Results;

namespace Aynesil.Application.Common.Validation;

/// <summary>
/// Handler içinde elle üretilen doğrulama hataları: kod (<c>ErrorCode</c>) + istek dilinde mesaj.
/// Mesaj, isteğin kültüründe üretim anında çözülür (kültür middleware tarafından handler'dan önce ayarlanır).
/// </summary>
public static class ValidationFailures
{
    public static ValidationFailure For(string property, MsgKey key, params object?[] args) =>
        new(property, Msg.Get(key, args)) { ErrorCode = key.Value };
}
