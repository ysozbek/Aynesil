using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Notifications.Commands;
using Aynesil.Application.Features.Notifications.Dtos;
using Aynesil.Application.Features.Notifications.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Notification inbox — personal notifications for the authenticated user.
/// Route: /api/notifications
/// </summary>
[Route("api/notifications")]
public sealed class NotificationsController : BaseController
{
    /// <summary>Returns the authenticated user's notification inbox (paginated).</summary>
    [HttpGet]
    [HasPermission(Permissions.Notifications.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NotificationListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] GetMyNotificationsQuery query, CancellationToken ct)
    {
        query.RecipientUserId = GetCurrentUserId();
        return OkResult(await Sender.Send(query, ct));
    }

    /// <summary>Returns the count of unread notifications for the authenticated user.</summary>
    [HttpGet("unread-count")]
    [HasPermission(Permissions.Notifications.Read)]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        => OkResult(await Sender.Send(new GetUnreadCountQuery(GetCurrentUserId()), ct));

    /// <summary>Marks a specific notification as read.</summary>
    [HttpPatch("{id:guid}/read")]
    [HasPermission(Permissions.Notifications.Read)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await Sender.Send(new MarkNotificationReadCommand(id, GetCurrentUserId()), ct);
        return NoContentResult();
    }

    /// <summary>Marks all notifications as read for the authenticated user.</summary>
    [HttpPatch("mark-all-read")]
    [HasPermission(Permissions.Notifications.Read)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        => OkResult(await Sender.Send(new MarkAllNotificationsReadCommand(GetCurrentUserId()), ct));

    // ── Preferences ──────────────────────────────────────────────────────────────

    /// <summary>Returns the authenticated user's notification preferences.</summary>
    [HttpGet("preferences")]
    [HasPermission(Permissions.Notifications.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationPreferenceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
        => OkResult(await Sender.Send(new GetNotificationPreferencesQuery(GetCurrentUserId()), ct));

    /// <summary>Updates the authenticated user's notification preferences.</summary>
    [HttpPut("preferences")]
    [HasPermission(Permissions.Notifications.Read)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesCommand command, CancellationToken ct)
    {
        var cmd = command with { UserId = GetCurrentUserId() };
        await Sender.Send(cmd, ct);
        return NoContentResult();
    }

    // ── Helper ────────────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = HttpContext.User.FindFirst("sub")?.Value
                 ?? HttpContext.User.FindFirst("userId")?.Value;
        if (!Guid.TryParse(claim, out var id))
            throw new UnauthorizedAccessException("Authenticated user identity not found.");
        return id;
    }
}
