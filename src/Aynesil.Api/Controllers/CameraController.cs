using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Cameras.Commands;
using Aynesil.Application.Features.Cameras.Dtos;
using Aynesil.Application.Features.Cameras.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Camera & Live Session Management.
/// Covers: camera CRUD, room/session assignments, viewing authorization workflow,
/// live/replay stream access workflow, and viewing history (audit log).
/// Route: /api/cameras
/// </summary>
[Route("api/cameras")]
public sealed class CameraController : BaseController
{
    // ── Camera CRUD ───────────────────────────────────────────────────────────────

    /// <summary>Paginated list of cameras. Filterable by campus, type, active status, and free-text.</summary>
    [HttpGet]
    [HasPermission(Permissions.Camera.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CameraListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCameras(
        [FromQuery] GetCamerasQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full camera detail including room and session assignments.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Camera.Read)]
    [ProducesResponseType(typeof(ApiResponse<CameraDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCamera(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetCameraQuery(id), ct));

    /// <summary>Register a new camera in the corporation.</summary>
    [HttpPost]
    [HasPermission(Permissions.Camera.Create)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterCamera(
        [FromBody] RegisterCameraCommand command, CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedResult(id, $"/api/cameras/{id}");
    }

    /// <summary>Update camera details (name, type, campus, stream configuration).</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Camera.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCamera(
        Guid id, [FromBody] UpdateCameraCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Soft-delete a camera. Existing assignments and logs are preserved.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Camera.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCamera(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteCameraCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Activate or deactivate a camera feed.</summary>
    [HttpPatch("{id:guid}/active")]
    [HasPermission(Permissions.Camera.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCameraActive(
        Guid id, [FromBody] SetCameraActiveRequest body, CancellationToken ct)
    {
        await Sender.Send(new SetCameraActiveCommand(id, body.IsActive), ct);
        return NoContentResult();
    }

    // ── Room Assignments ──────────────────────────────────────────────────────────

    /// <summary>Get all camera assignments for a room or session.</summary>
    [HttpGet("assignments")]
    [HasPermission(Permissions.CameraAssignment.Read)]
    [ProducesResponseType(typeof(ApiResponse<CameraAssignmentsResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] GetCameraAssignmentsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Assign a camera to a room.</summary>
    [HttpPost("{id:guid}/rooms")]
    [HasPermission(Permissions.CameraAssignment.Manage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToRoom(
        Guid id, [FromBody] AssignCameraToRoomRequest body, CancellationToken ct)
    {
        var assignmentId = await Sender.Send(
            new AssignCameraToRoomCommand(body.CorporationId, id, body.RoomId), ct);
        return CreatedResult(assignmentId, $"/api/cameras/assignments");
    }

    /// <summary>Remove a camera from a room.</summary>
    [HttpDelete("{id:guid}/rooms/{roomId:guid}")]
    [HasPermission(Permissions.CameraAssignment.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignFromRoom(Guid id, Guid roomId, CancellationToken ct)
    {
        await Sender.Send(new UnassignCameraFromRoomCommand(id, roomId), ct);
        return NoContentResult();
    }

    /// <summary>Assign a camera to a session.</summary>
    [HttpPost("{id:guid}/sessions")]
    [HasPermission(Permissions.CameraAssignment.Manage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToSession(
        Guid id, [FromBody] AssignCameraToSessionRequest body, CancellationToken ct)
    {
        var assignmentId = await Sender.Send(
            new AssignCameraToSessionCommand(body.CorporationId, id, body.SessionId), ct);
        return CreatedResult(assignmentId, $"/api/cameras/assignments");
    }

    /// <summary>Remove a camera from a session.</summary>
    [HttpDelete("{id:guid}/sessions/{sessionId:guid}")]
    [HasPermission(Permissions.CameraAssignment.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignFromSession(Guid id, Guid sessionId, CancellationToken ct)
    {
        await Sender.Send(new UnassignCameraFromSessionCommand(id, sessionId), ct);
        return NoContentResult();
    }

    // ── Viewing Authorization Workflow ────────────────────────────────────────────

    /// <summary>
    /// Paginated list of viewing authorizations.
    /// Filterable by guardian, student, session, and active-only flag.
    /// </summary>
    [HttpGet("authorizations")]
    [HasPermission(Permissions.ViewingAuthorization.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ViewingAuthorizationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthorizations(
        [FromQuery] GetViewingAuthorizationsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Get a single viewing authorization by ID.</summary>
    [HttpGet("authorizations/{id:guid}")]
    [HasPermission(Permissions.ViewingAuthorization.Read)]
    [ProducesResponseType(typeof(ApiResponse<ViewingAuthorizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuthorization(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetViewingAuthorizationQuery(id), ct));

    /// <summary>
    /// Grant a time-limited viewing authorization to a guardian.
    /// Validates: camera_viewing KVKK consent + guardian_portal_access.can_view_camera.
    /// ValidTo is mandatory.
    /// </summary>
    [HttpPost("authorizations")]
    [HasPermission(Permissions.ViewingAuthorization.Grant)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GrantAuthorization(
        [FromBody] GrantViewingAuthorizationCommand command, CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedResult(id, $"/api/cameras/authorizations/{id}");
    }

    /// <summary>Revoke a viewing authorization immediately. Security event is logged.</summary>
    [HttpPost("authorizations/{id:guid}/revoke")]
    [HasPermission(Permissions.ViewingAuthorization.Revoke)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAuthorization(Guid id, CancellationToken ct)
    {
        await Sender.Send(new RevokeViewingAuthorizationCommand(id), ct);
        return NoContentResult();
    }

    // ── Stream / Viewing Workflow ─────────────────────────────────────────────────

    /// <summary>
    /// Start a viewing session (open a viewing_log entry).
    /// Validates authorization validity and active consent.
    /// Returns the composite (LogId, StartedAt) key needed to end the session.
    /// </summary>
    [HttpPost("viewing/start")]
    [HasPermission(Permissions.ViewingSession.Start)]
    [ProducesResponseType(typeof(ApiResponse<StartViewingSessionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartViewing(
        [FromBody] StartViewingSessionCommand command, CancellationToken ct)
        => OkResult(await Sender.Send(command, ct));

    /// <summary>End a viewing session (close the viewing_log entry). Duration is recorded.</summary>
    [HttpPost("viewing/{logId:long}/end")]
    [HasPermission(Permissions.ViewingSession.End)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndViewing(
        long logId, [FromBody] EndViewingSessionRequest body, CancellationToken ct)
    {
        await Sender.Send(new EndViewingSessionCommand(logId, body.StartedAt), ct);
        return NoContentResult();
    }

    // ── Viewing History / Audit Log ───────────────────────────────────────────────

    /// <summary>
    /// Paginated viewing log (audit trail). Filterable by guardian, session, camera, date range.
    /// Immutable append-only records — cannot be edited or deleted.
    /// </summary>
    [HttpGet("viewing-logs")]
    [HasPermission(Permissions.ViewingLog.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ViewingLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetViewingLogs(
        [FromQuery] GetViewingLogsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));
}

// ── Inline request body models ────────────────────────────────────────────────

public record SetCameraActiveRequest(bool IsActive);
public record AssignCameraToRoomRequest(Guid CorporationId, Guid RoomId);
public record AssignCameraToSessionRequest(Guid CorporationId, Guid SessionId);
public record EndViewingSessionRequest(DateTimeOffset StartedAt);
