using System.Globalization;
using System.Text.Json;
using Aynesil.Api.Middleware;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Tests.Api;

/// <summary>
/// Smoke-test bulgusu: tanınmayan istisnalar (InvalidOperationException dahil) 409 değil 500 dönmeli
/// ve yanıta istisna metni / EF sorgusu / stack trace sızmamalı. Mesajlar istek diline (tr/en) göre çözülür.
/// </summary>
public class ExceptionMiddlewareTests
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr");
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");

    private static async Task<(int Status, string Body, JsonElement Json, CapturingLogger Log)> RunAsync(
        Exception ex, string? acceptLanguage = null)
    {
        var log = new CapturingLogger();
        var middleware = new ExceptionMiddleware(_ => throw ex, log);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        context.TraceIdentifier = "trace-123";
        if (acceptLanguage is not null) context.Request.Headers.AcceptLanguage = acceptLanguage;

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body, JsonDocument.Parse(body).RootElement, log);
    }

    [Theory]
    [InlineData("The LINQ expression 'DbSet<CampEnrollment>().OrderBy(c => new Dto(c.Id).Id)' could not be translated.")]
    [InlineData("Sequence contains no elements; SELECT * FROM iam.user_account WHERE password_hash = 'x'")]
    public async Task InvalidOperationException_is_500_and_does_not_leak(string secret)
    {
        var (status, body, json, log) = await RunAsync(new InvalidOperationException(secret));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        json.GetProperty("message").GetString().Should().Be(Msg.Get(MsgKeys.Error.Unexpected, Tr));
        json.GetProperty("code").GetString().Should().Be(MsgKeys.Error.Unexpected.Value);
        json.GetProperty("traceId").GetString().Should().Be("trace-123");
        body.Should().NotContain("LINQ").And.NotContain("SELECT").And.NotContain("password_hash")
            .And.NotContain("StackTrace").And.NotContain("   at ");
        log.Errors.Should().ContainSingle(e => e.Exception is InvalidOperationException);
    }

    [Fact]
    public async Task Unknown_exception_is_500_with_generic_message()
    {
        var (status, body, json, _) = await RunAsync(new NullReferenceException("secret detail"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        json.GetProperty("message").GetString().Should().Be(Msg.Get(MsgKeys.Error.Unexpected, Tr));
        body.Should().NotContain("secret detail");
    }

    [Fact]
    public async Task DbUpdateConcurrencyException_is_409()
    {
        var (status, _, json, _) = await RunAsync(new DbUpdateConcurrencyException("row_version mismatch"));
        status.Should().Be(StatusCodes.Status409Conflict);
        json.GetProperty("code").GetString().Should().Be(MsgKeys.Error.Concurrency.Value);
    }

    [Fact]
    public async Task Same_exception_is_localized_by_Accept_Language()
    {
        var (_, _, tr, _) = await RunAsync(new ForbiddenAccessException(), "tr");
        var (_, _, en, _) = await RunAsync(new ForbiddenAccessException(), "en-US,en;q=0.9");

        tr.GetProperty("message").GetString().Should().Be(Msg.Get(MsgKeys.Error.Forbidden, Tr));
        en.GetProperty("message").GetString().Should().Be(Msg.Get(MsgKeys.Error.Forbidden, En));
        tr.GetProperty("message").GetString().Should().NotBe(en.GetProperty("message").GetString());
        tr.GetProperty("code").GetString().Should().Be(en.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Coded_BusinessRuleViolation_resolves_text_and_parameters_in_request_language()
    {
        var ex = new BusinessRuleViolationException(MsgKeys.Error.NotFound, "Student");
        var (status, _, json, _) = await RunAsync(ex, "en");

        status.Should().Be(StatusCodes.Status409Conflict);
        json.GetProperty("message").GetString().Should().Be("Student was not found.");
        json.GetProperty("code").GetString().Should().Be(MsgKeys.Error.NotFound.Value);
    }

    [Fact]
    public async Task Status_and_number_arguments_are_rendered_in_the_request_language()
    {
        var status = new BusinessRuleViolationException(MsgKeys.Rule.PlanCancelStatus, new EnumValue("Status", "draft"));
        var (_, _, tr, _) = await RunAsync(status, "tr");
        var (_, _, en, _) = await RunAsync(status, "en");
        tr.GetProperty("message").GetString().Should().Be("'taslak' durumundaki bir plan iptal edilemez.");
        en.GetProperty("message").GetString().Should().Be("A 'draft' plan cannot be cancelled.");

        var money = new BusinessRuleViolationException(MsgKeys.Rule.AdjustmentNegativeBalance, new Formatted(-5m, "F2"));
        var (_, _, trMoney, _) = await RunAsync(money, "tr");
        var (_, _, enMoney, _) = await RunAsync(money, "en");
        trMoney.GetProperty("message").GetString().Should().Contain("-5,00");
        enMoney.GetProperty("message").GetString().Should().Contain("-5.00");
    }

    [Fact]
    public async Task NotFoundException_names_the_entity_in_the_request_language()
    {
        var ex = new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Student"));
        var (statusTr, _, tr, _) = await RunAsync(ex, "tr");
        var (_, _, en, _) = await RunAsync(ex, "en");

        statusTr.Should().Be(StatusCodes.Status404NotFound);
        tr.GetProperty("message").GetString().Should().Be("Öğrenci bulunamadı.");
        en.GetProperty("message").GetString().Should().Be("Student was not found.");
        tr.GetProperty("code").GetString().Should().Be(MsgKeys.Error.NotFound.Value);
    }

    [Fact]
    public async Task Framework_KeyNotFoundException_returns_404_without_leaking_its_text()
    {
        var (status, body, json, _) = await RunAsync(new KeyNotFoundException("The given key 'secret-key' was not present in the dictionary."), "en");

        status.Should().Be(StatusCodes.Status404NotFound);
        body.Should().NotContain("secret-key");
        json.GetProperty("message").GetString().Should().Be("Record was not found.");
    }

    [Fact]
    public async Task InvalidArgumentException_carries_code_and_parameters()
    {
        var ex = new InvalidArgumentException(MsgKeys.Validation.MustBeAfter, MsgArg.Field("EndsAt"), MsgArg.Field("StartsAt"));
        var (status, _, tr, _) = await RunAsync(ex, "tr");
        var (_, _, en, _) = await RunAsync(ex, "en");

        status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        tr.GetProperty("code").GetString().Should().Be(MsgKeys.Validation.MustBeAfter.Value);
        tr.GetProperty("message").GetString().Should().Be("Bitiş, Başlangıç alanından sonra olmalıdır.");
        en.GetProperty("message").GetString().Should().Be("Ends at must be after Starts at.");
    }

    [Fact]
    public async Task ConflictException_without_code_keeps_its_own_message()
    {
        var (status, _, json, _) = await RunAsync(new ConflictException("Zaten kayıtlı."));
        status.Should().Be(StatusCodes.Status409Conflict);
        json.GetProperty("message").GetString().Should().Be("Zaten kayıtlı.");
        json.GetProperty("code").GetString().Should().Be(MsgKeys.Error.BusinessRule.Value);
    }

    [Fact]
    public async Task BusinessRuleViolationException_without_code_keeps_its_own_message()
    {
        var (status, _, json, _) = await RunAsync(new BusinessRuleViolationException("Geçersiz durum geçişi."));
        status.Should().Be(StatusCodes.Status409Conflict);
        json.GetProperty("message").GetString().Should().Be("Geçersiz durum geçişi.");
    }

    [Fact]
    public async Task ArgumentException_from_domain_guard_is_422_generic_and_does_not_leak()
    {
        var (status, body, json, log) = await RunAsync(new ArgumentException("End date cannot be before start date."), "en");

        status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        json.GetProperty("code").GetString().Should().Be(MsgKeys.Error.InvalidArgument.Value);
        json.GetProperty("message").GetString().Should().Be(Msg.Get(MsgKeys.Error.InvalidArgument, En));
        body.Should().NotContain("End date");
        log.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Exception is ArgumentException);
    }

    [Fact]
    public async Task ArgumentNullException_stays_500()
    {
        var (status, _, _, _) = await RunAsync(new ArgumentNullException("x"));
        status.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ValidationException_keeps_errors_shape_and_adds_code_and_errorDetails()
    {
        var failures = new[]
        {
            new ValidationFailure("Email", "E-posta zorunludur.") { ErrorCode = "NotEmptyValidator" },
            new ValidationFailure("Lines[0].Amount", "Geçersiz.") // kodsuz → Error.InvalidValue
        };
        var (status, _, json, _) = await RunAsync(new ValidationException(failures), "en");

        status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        json.GetProperty("code").GetString().Should().Be(MsgKeys.Error.ValidationFailed.Value);
        json.GetProperty("message").GetString().Should().Be(Msg.Get(MsgKeys.Error.ValidationFailed, En));

        // Geriye uyumlu biçim: alan → string[]
        json.GetProperty("errors").GetProperty("email")[0].GetString().Should().Be("E-posta zorunludur.");
        json.GetProperty("errors").GetProperty("lines[0].amount")[0].GetString().Should().Be("Geçersiz.");

        // Yeni: alan → [{ code, message }]
        var detail = json.GetProperty("errorDetails").GetProperty("email")[0];
        detail.GetProperty("code").GetString().Should().Be("NotEmptyValidator");
        detail.GetProperty("message").GetString().Should().Be("E-posta zorunludur.");
        json.GetProperty("errorDetails").GetProperty("lines[0].amount")[0].GetProperty("code").GetString()
            .Should().Be(ValidationException.DefaultErrorCode);
    }

    private sealed class CapturingLogger : ILogger<ExceptionMiddleware>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];
        public IEnumerable<(LogLevel Level, Exception? Exception)> Errors => Entries.Where(e => e.Level == LogLevel.Error);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullLogger.Instance.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, exception));
    }
}
