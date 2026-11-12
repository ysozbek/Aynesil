using System.Globalization;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Application.Features.Scheduling.Queries;
using FluentAssertions;

namespace Aynesil.Tests.Localization;

/// <summary>Doğrulama mesajlarının istek diline göre değiştiğini ve hata kodunun sabit kaldığını doğrular.</summary>
public class ValidationLocalizationTests
{
    static ValidationLocalizationTests() => ValidationLocalization.Configure();

    private static T InCulture<T>(string locale, Func<T> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        try { return action(); }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    private static readonly CreateCalendarEntryCommand Invalid = new(
        "Tatil", "bogus", new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero), false, null);

    [Fact]
    public void Same_invalid_command_gets_different_messages_but_the_same_codes_in_tr_and_en()
    {
        var tr = InCulture("tr", () => new CreateCalendarEntryCommandValidator().Validate(Invalid).Errors);
        var en = InCulture("en", () => new CreateCalendarEntryCommandValidator().Validate(Invalid).Errors);

        tr.Select(e => e.ErrorCode).Should().BeEquivalentTo(
            [MsgKeys.Validation.MustBeOneOf.Value, MsgKeys.Validation.MustBeAfter.Value]);
        en.Select(e => e.ErrorCode).Should().Equal(tr.Select(e => e.ErrorCode));

        // Kod değerleri ham değil, o dildeki etiketle; alan adları da yerelleştirilmiş.
        tr.Select(e => e.ErrorMessage).Should().Contain(
            "Kayıt türü şu değerlerden biri olmalıdır: tatil, kapanış, etkinlik, dönem arası.");
        en.Select(e => e.ErrorMessage).Should().Contain(
            "Entry type must be one of: holiday, closure, event, term break.");
        en.Select(e => e.ErrorMessage).Should().Contain("Ends at must be after Starts at.");
        tr.Select(e => e.ErrorMessage).Should().NotContain(m => m.Contains("term_break"));
    }

    [Fact]
    public void Required_query_parameters_use_localized_field_labels()
    {
        var q = new GetSchoolCalendarQuery(default, default);
        var tr = InCulture("tr", () => new GetSchoolCalendarQueryValidator().Validate(q).Errors.Select(e => e.ErrorMessage).ToList());
        var en = InCulture("en", () => new GetSchoolCalendarQueryValidator().Validate(q).Errors.Select(e => e.ErrorMessage).ToList());

        tr.Should().Contain("Başlangıç zorunludur.");
        en.Should().Contain("Start is required.");
    }

    [Fact]
    public void Library_default_messages_follow_the_request_culture_and_use_localized_names()
    {
        var empty = Invalid with { Title = "" };
        var tr = InCulture("tr", () => new CreateCalendarEntryCommandValidator().Validate(empty).Errors.Select(e => e.ErrorMessage).ToList());
        var en = InCulture("en", () => new CreateCalendarEntryCommandValidator().Validate(empty).Errors.Select(e => e.ErrorMessage).ToList());

        tr.Should().Contain(m => m.Contains("Başlık"));
        en.Should().Contain(m => m.Contains("Title"));
    }
}
