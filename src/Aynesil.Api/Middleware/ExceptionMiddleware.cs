using System.Text.Json;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Shared;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Api.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches all unhandled exceptions and transforms them into consistent ApiResponse envelopes.
/// Never leaks stack traces or internal details to clients in production.
/// Writes all unexpected exceptions to Serilog structured logging with a correlation TraceId.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
                ApiResponse.Fail("Validation failed.", ve.Errors)),

            NotFoundException nfe => (
                StatusCodes.Status404NotFound,
                ApiResponse.Fail(nfe.Message)),

            ForbiddenAccessException => (
                StatusCodes.Status403Forbidden,
                ApiResponse.Fail("You do not have permission to perform this action.")),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                ApiResponse.Fail("Authentication required.")),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                ApiResponse.Fail("The record was modified by another user. Please refresh and try again.")),

            OperationCanceledException => (
                StatusCodes.Status499ClientClosedRequest,
                ApiResponse.Fail("The request was cancelled.")),

            _ => (
                StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(_env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again."))
        };

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
}
