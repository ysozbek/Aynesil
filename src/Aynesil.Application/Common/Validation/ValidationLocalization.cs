using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;

namespace Aynesil.Application.Common.Validation;

/// <summary>
/// FluentValidation mesajlarını global olarak Türkçe yapar ve özellik adlarını
/// okunur hale getirir (sözlük + PascalCase ayrıştırma).
/// </summary>
public static class ValidationLocalization
{
    private static readonly object Gate = new();
    private static bool _configured;

    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Code"] = "Kod",
        ["Name"] = "Ad",
        ["FirstName"] = "Ad",
        ["LastName"] = "Soyad",
        ["FullName"] = "Ad Soyad",
        ["Title"] = "Başlık",
        ["Description"] = "Açıklama",
        ["Notes"] = "Not",
        ["Note"] = "Not",
        ["Email"] = "E-posta",
        ["Phone"] = "Telefon",
        ["Username"] = "Kullanıcı adı",
        ["Password"] = "Parola",
        ["CorporationId"] = "Kurum",
        ["CampusId"] = "Kampüs",
        ["StudentId"] = "Öğrenci",
        ["EducatorId"] = "Eğitimci",
        ["GuardianId"] = "Veli",
        ["RoomId"] = "Oda",
        ["SessionId"] = "Seans",
        ["InvoiceId"] = "Fatura",
        ["PaymentMethodId"] = "Ödeme yöntemi",
        ["PackageDefinitionId"] = "Paket tanımı",
        ["CampPeriodId"] = "Kamp dönemi",
        ["StartDate"] = "Başlangıç tarihi",
        ["EndDate"] = "Bitiş tarihi",
        ["StartTime"] = "Başlangıç saati",
        ["EndTime"] = "Bitiş saati",
        ["BirthDate"] = "Doğum tarihi",
        ["Amount"] = "Tutar",
        ["Quantity"] = "Adet",
        ["Status"] = "Durum",
        ["Page"] = "Sayfa",
        ["PageSize"] = "Sayfa boyutu",
    };

    private static readonly Dictionary<string, string> Words = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Id"] = "Kimlik", ["Date"] = "tarihi", ["Time"] = "saati", ["Start"] = "Başlangıç",
        ["End"] = "Bitiş", ["Type"] = "Tür", ["Number"] = "No", ["Count"] = "Sayısı",
        ["Total"] = "Toplam", ["Price"] = "Fiyat", ["Address"] = "Adres", ["Url"] = "Bağlantı",
    };

    public static void Configure()
    {
        lock (Gate)
        {
            if (_configured) return;
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("tr");
            ValidatorOptions.Global.DisplayNameResolver = (_, member, _) =>
                member is null ? null : Humanize(member.Name);
            _configured = true;
        }
    }

    internal static string Humanize(string name)
    {
        if (Names.TryGetValue(name, out var known)) return known;

        var parts = Regex.Split(name, @"(?<=[a-z0-9])(?=[A-Z])|_")
            .Where(p => p.Length > 0)
            .Select(p => Words.TryGetValue(p, out var w) ? w : p)
            .ToArray();
        return parts.Length == 0 ? name : string.Join(' ', parts);
    }
}
