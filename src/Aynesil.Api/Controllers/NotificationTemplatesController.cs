using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Notifications.Commands;
using Aynesil.Application.Features.Notifications.Dtos;
using Aynesil.Application.Features.Notifications.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Notification template management — admin/content-staff.
/// Route: /api/notification-templates
/// </summary>
[Route("api/notification-templates")]
public sealed class NotificationTemplatesController : BaseController
{
    /// <summary>List notification templates (paginated, filterable).</summary>
    [HttpGet]
    [HasPermission(Permissions.NotificationTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NotificationTemplateListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] GetNotificationTemplatesQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Get a single notification template with all translations.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.NotificationTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetNotificationTemplateQuery(id), ct));

    /// <summary>Create a new notification template with localized translations.</summary>
    [HttpPost]
    [HasPermission(Permissions.NotificationTemplates.Create)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateNotificationTemplateCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/notification-templates/{result.Id}");
    }

    /// <summary>Update an existing notification template.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.NotificationTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(
        Guid id, [FromBody] UpdateNotificationTemplateCommand command, CancellationToken ct)
    {
        var cmd = command with { Id = id };
        return OkResult(await Sender.Send(cmd, ct));
    }

    /// <summary>Deactivate or delete a notification template.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.NotificationTemplates.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteNotificationTemplateCommand(id), ct);
        return NoContentResult();
    }
}
