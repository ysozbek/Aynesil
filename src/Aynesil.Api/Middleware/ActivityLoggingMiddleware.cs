using Aynesil.Application.Common.Interfaces;
using Aynesil.Infrastructure.Persistence;
using Aynesil.Domain.Modules.Core.Entities;

namespace Aynesil.Api.Middleware;

/// <summary>
/// Records user activity events to core.activity_log for audit and analytics.
/// Only logs authenticated GET requests for "view" events and mutation methods for "action" events.
/// Does NOT log health checks, static files, or Scalar/OpenAPI endpoints.
/// </summary>
public class ActivityLoggingMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> _skipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/ready", "/metrics", "/scalar", "/openapi"
    };

    public ActivityLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AynesilDbContext db, ICurrentUserService currentUser)
    {
        await _next(context);

        if (!currentUser.IsAuthenticated) return;
        if (context.Response.StatusCode >= 400) return;
        if (_skipPaths.Any(p => context.Request.Path.StartsWithSegments(p))) return;

        var method = context.Request.Method;
        var activityType = method switch
        {
            "GET" => "view",
            "POST" => "create",
            "PUT" or "PATCH" => "update",
            "DELETE" => "delete",
            _ => null
        };

        if (activityType is null) return;

        try
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                CorporationId = currentUser.Claims.TryGetValue("corporation_id", out var c)
                    ? Guid.TryParse(c, out var cid) ? cid : null
                    : null,
                UserId = currentUser.UserId,
                ActivityType = activityType,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                OccurredAt = DateTimeOffset.UtcNow,
                Context = System.Text.Json.JsonSerializer.Serialize(new
                {
                    path = context.Request.Path.Value,
                    method
                })
            });
            await db.SaveChangesAsync();
        }
        catch
        {
            // Never fail a request due to activity logging errors
        }
    }
}
