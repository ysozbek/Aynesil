using System.Text.Json;
using Aynesil.Api.Middleware;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aynesil.Tests.Api;

/// <summary>
/// Smoke-test bulgusu: tanınmayan istisnalar (InvalidOperationException dahil) 409 değil 500 dönmeli
/// ve yanıta istisna metni / EF sorgusu / stack trace sızmamalı.
/// </summary>
public class ExceptionMiddlewareTests
{
    private const string GenericMessage = "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";

    private static async Task<(int Status, string Body, JsonElement Json, CapturingLogger Log)> RunAsync(Exception ex)
    {
        var log = new CapturingLogger();
        var middleware = new ExceptionMiddleware(_ => throw ex, log);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        context.TraceIdentifier = "trace-123";

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
        json.GetProperty("message").GetString().Should().Be(GenericMessage);
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
        json.GetProperty("message").GetString().Should().Be(GenericMessage);
        body.Should().NotContain("secret detail");
    }

    [Fact]
    public async Task DbUpdateConcurrencyException_is_409()
    {
        var (status, _, _, _) = await RunAsync(new DbUpdateConcurrencyException("row_version mismatch"));
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task ConflictException_is_409_with_its_own_message()
    {
        var (status, _, json, _) = await RunAsync(new ConflictException("Zaten kayıtlı."));
        status.Should().Be(StatusCodes.Status409Conflict);
        json.GetProperty("message").GetString().Should().Be("Zaten kayıtlı.");
    }

    [Fact]
    public async Task BusinessRuleViolationException_is_409_with_its_own_message()
    {
        var (status, _, json, _) = await RunAsync(new BusinessRuleViolationException("Geçersiz durum geçişi."));
        status.Should().Be(StatusCodes.Status409Conflict);
        json.GetProperty("message").GetString().Should().Be("Geçersiz durum geçişi.");
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
