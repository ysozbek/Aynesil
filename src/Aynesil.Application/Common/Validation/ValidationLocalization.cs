using FluentValidation;
using Aynesil.Application.Common.Localization;

namespace Aynesil.Application.Common.Validation;

/// <summary>
/// FluentValidation'ı istek diline bağlar: kütüphane kültürü global olarak sabitlenmez
/// (<c>LanguageManager.Culture</c> null → <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>),
/// özellik adları <c>FieldNames.resx</c> kaynağından (tr/en) çözülür.
/// Doğrulayıcılar scoped kayıtlıdır; görünen ad istek kültüründe, istek başına üretilir.
/// </summary>
public static class ValidationLocalization
{
    private static readonly object Gate = new();
    private static bool _configured;

    public static void Configure()
    {
        lock (Gate)
        {
            if (_configured) return;
            ValidatorOptions.Global.LanguageManager.Culture = null;
            ValidatorOptions.Global.DisplayNameResolver = (_, member, _) =>
                member is null ? null : Msg.Field(member.Name);
            _configured = true;
        }
    }
}
