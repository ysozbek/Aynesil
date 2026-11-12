using System.Text.Json;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Shared;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Api.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches all unhandled exceptions and transforms them into consistent ApiResponse envelopes.
/// Never leaks exception text, EF queries or stack traces to clients, in any environment:
/// unrecognised exceptions become a generic 500 carrying only a Turkish message and the TraceId,
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

        var (statusCode, response) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                ApiResponse.Fail("Doğrulama hatası.", ErrorKeyFormatter.Normalize(ve.Errors))),

            NotFoundException nfe => (
                StatusCodes.Status404NotFound,
                ApiResponse.Fail(nfe.Message)),

            KeyNotFoundException knf => (
                StatusCodes.Status404NotFound,
                ApiResponse.Fail(knf.Message)),

            ForbiddenAccessException => (
                StatusCodes.Status403Forbidden,
                ApiResponse.Fail("Bu işlem için yetkiniz yok.")),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                ApiResponse.Fail("Oturum açmanız gerekiyor.")),

            // The application's own conflict exceptions: ConflictException (derives from it) and the
            // BusinessRuleViolationException thrown by domain entities / handlers for invalid
            // transitions, duplicates, wrong state, ... Their messages are authored by us and safe to show.
            // InvalidOperationException is deliberately NOT handled here: framework/system errors
            // (including EF query-translation failures) must fall through to the generic 500 below.
            BusinessRuleViolationException bre => (
                StatusCodes.Status409Conflict,
                ApiResponse.Fail(bre.Message)),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                ApiResponse.Fail("Kayıt başka bir kullanıcı tarafından değiştirildi. Lütfen sayfayı yenileyip tekrar deneyin.")),

            DbUpdateException dbe when IsUniqueViolation(dbe) => (
                StatusCodes.Status409Conflict,
                ApiResponse.Fail("Aynı anahtara sahip bir kayıt zaten mevcut.")),

            // 23503 from a composite tenant FK or the global-or-tenant reference trigger (#86).
            // Raw SQL (ExecuteSqlRaw) surfaces the PostgresException without a DbUpdateException.
            _ when PostgresErrorClassifier.ClassifyForeignKeyViolation(exception, out _)
                   == PostgresErrorClassifier.ForeignKeyViolation.MissingOrForeignReference => (
                StatusCodes.Status422UnprocessableEntity,
                ApiResponse.Fail("İlişkili kayıt bulunamadı veya bu kuruma ait değil.")),

            _ when PostgresErrorClassifier.ClassifyForeignKeyViolation(exception, out _)
                   == PostgresErrorClassifier.ForeignKeyViolation.StillReferenced => (
                StatusCodes.Status409Conflict,
                ApiResponse.Fail("Bu kayıt başka kayıtlarda kullanıldığı için silinemez.")),

            OperationCanceledException => (
                StatusCodes.Status499ClientClosedRequest,
                ApiResponse.Fail("İstek iptal edildi.")),

            _ => (
                StatusCodes.Status500InternalServerError,
                ApiResponse.Fail("Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin."))
        };

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
