using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Notifications.Commands;
using Aynesil.Application.Features.Notifications.Dtos;
using Aynesil.Application.Features.Notifications.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Notification trigger configuration — admin only.
/// Business users configure which template fires for each trigger event,
/// with what timing offset, and via which channels.
/// Route: /api/notification-triggers
/// </summary>
[Route("api/notification-triggers")]
public sealed class NotificationTriggersController : BaseController
{
    /// <summary>List trigger configurations (paginated).</summary>
    [HttpGet]
    [HasPermission(Permissions.NotificationTriggers.Manage)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NotificationTriggerConfigListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTriggerConfigs(
        [FromQuery] GetTriggerConfigsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>
    /// Create or update a trigger config for the given (corporationId, triggerCode) pair.
    /// Channels list replaces the existing channel set.
    /// </summary>
    [HttpPut]
    [HasPermission(Permissions.NotificationTriggers.Manage)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTriggerConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertTriggerConfig(
        [FromBody] UpsertNotificationTriggerConfigCommand command, CancellationToken ct)
        => OkResult(await Sender.Send(command, ct));

    /// <summary>Delete a trigger configuration.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.NotificationTriggers.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTriggerConfig(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteNotificationTriggerConfigCommand(id), ct);
        return NoContentResult();
    }
}
