using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using FluentValidation;

namespace Aynesil.Application.Common.Validation;

public static class RuleBuilderLocalizationExtensions
{
    /// <summary>
    /// Sabit metin yerine kaynak anahtarı kullanır: anahtar hem hata kodu (<c>ErrorCode</c>) hem de
    /// doğrulama anındaki kültürde çözülen mesajdır. <c>{0}</c>… parametreleri için args verilir
    /// (kod listeleri <see cref="Msg.EnumList"/> ile ertelenmiş etiket olarak geçirilir).
    /// {PropertyName} gibi FluentValidation yer tutucuları mesajda kullanılabilir.
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> WithKey<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, MsgKey key, params object?[] args) =>
        rule.WithErrorCode(key.Value).WithMessage(_ => Msg.Get(key, args));
}
