using System.Text.Json;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using Aynesil.Shared;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Api.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches all unhandled exceptions and transforms them into consistent ApiResponse envelopes.
/// Never leaks exception text, EF queries or stack traces to clients, in any environment:
/// unrecognised exceptions become a generic 500 carrying only a localized (tr/en) message, a code and the TraceId,
/// and the details go to the structured log (Serilog) under that same TraceId.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        // İç middleware'lerin AsyncLocal değişiklikleri buraya yansımaz; metni istek dilinde üretmek için
        // kültür (Accept-Language → JWT locale → tr) bu noktada yeniden uygulanır.
        RequestCultureMiddleware.Apply(context);

        var (statusCode, response) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                ApiResponse.Fail(
                    Msg.Get(MsgKeys.Error.ValidationFailed), MsgKeys.Error.ValidationFailed.Value,
                    ErrorKeyFormatter.Normalize(ve.Errors),
                    ErrorKeyFormatter.Normalize(ve.ErrorDetails))),

            // Kodlu istisnalar metnini kod + parametrelerden alır; kodsuz (geçiş dönemi) olanlar kendi metnini döner.
            NotFoundException nfe => (
                StatusCodes.Status404NotFound,
                Localized(nfe, MsgKeys.Error.NotFound, nfe.Message)),

            KeyNotFoundException knf => (
                StatusCodes.Status404NotFound,
                Fail(MsgKeys.Error.NotFound, knf.Message)),

            ForbiddenAccessException fae => (
                StatusCodes.Status403Forbidden,
                Localized(fae, MsgKeys.Error.Forbidden, null)),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                Fail(MsgKeys.Error.Unauthorized)),

            // The application's own conflict exceptions: ConflictException (derives from it) and the
            // BusinessRuleViolationException thrown by domain entities / handlers for invalid
            // transitions, duplicates, wrong state, ... Their messages are authored by us and safe to show.
            // InvalidOperationException is deliberately NOT handled here: framework/system errors
            // (including EF query-translation failures) must fall through to the generic 500 below.
            BusinessRuleViolationException bre => (
                StatusCodes.Status409Conflict,
                Localized(bre, MsgKeys.Error.BusinessRule, bre.Message)),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                Fail(MsgKeys.Error.Concurrency)),

            DbUpdateException dbe when IsUniqueViolation(dbe) => (
                StatusCodes.Status409Conflict,
                Fail(MsgKeys.Error.DuplicateKey)),

            // 23503 from a composite tenant FK or the global-or-tenant reference trigger (#86).
            // Raw SQL (ExecuteSqlRaw) surfaces the PostgresException without a DbUpdateException.
            _ when PostgresErrorClassifier.ClassifyForeignKeyViolation(exception, out _)
                   == PostgresErrorClassifier.ForeignKeyViolation.MissingOrForeignReference => (
                StatusCodes.Status422UnprocessableEntity,
                Fail(MsgKeys.Error.ReferenceMissing)),

            _ when PostgresErrorClassifier.ClassifyForeignKeyViolation(exception, out _)
                   == PostgresErrorClassifier.ForeignKeyViolation.StillReferenced => (
                StatusCodes.Status409Conflict,
                Fail(MsgKeys.Error.ReferenceInUse)),

            // Domain guard'ları (tarih sırası, tutar vb.) ArgumentException atar; istemciye yalnızca genel mesaj
            // gider (istisna metni sızmaz), ayrıntı log'da. ArgumentNullException programcı hatasıdır → 500.
            ArgumentException and not ArgumentNullException => (
                StatusCodes.Status422UnprocessableEntity,
                Fail(MsgKeys.Error.InvalidArgument)),

            OperationCanceledException => (
                StatusCodes.Status499ClientClosedRequest,
                Fail(MsgKeys.Error.RequestCancelled)),

            _ => (
                StatusCodes.Status500InternalServerError,
                Fail(MsgKeys.Error.Unexpected))
        };

        if (exception is ArgumentException and not ArgumentNullException)
        {
            _logger.LogWarning(exception,
                "Domain argument guard rejected input. TraceId={TraceId} Path={Path}",
                traceId, context.Request.Path);
        }

        if (PostgresErrorClassifier.ClassifyForeignKeyViolation(exception, out var fkConstraint)
            != PostgresErrorClassifier.ForeignKeyViolation.None)
        {
            // Client gets a generic message; the constraint name stays in the logs for diagnosis.
            _logger.LogWarning("Foreign key violation on {Constraint}. TraceId={TraceId} Path={Path}",
                fkConstraint, traceId, context.Request.Path);
        }

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception,
                "Unhandled exception. TraceId={TraceId} Path={Path}",
                traceId, context.Request.Path);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = response with { TraceId = traceId };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, _jsonOptions));
    }

    private static ApiResponse Fail(MsgKey key, string? overrideMessage = null) =>
        ApiResponse.Fail(overrideMessage ?? Msg.Get(key), key.Value);

    /// <summary>Kodlu istisna → kod + çözülmüş metin; kodsuz → varsayılan kod ve (varsa) istisnanın kendi metni.</summary>
    private static ApiResponse Localized(ILocalizableException ex, MsgKey fallbackCode, string? legacyMessage)
    {
        if (ex.Code is { } code)
            return ApiResponse.Fail(Msg.Get(code, ex.Args), code.Value);
        return Fail(fallbackCode, legacyMessage);
    }

    /// <summary>Postgres unique_violation (23505) surfaced through EF SaveChanges.</summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            var sqlState = inner.GetType().GetProperty("SqlState")?.GetValue(inner) as string;
            if (sqlState == "23505")
                return true;
        }
        return false;
    }
}
