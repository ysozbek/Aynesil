using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Meetings.Commands;
using Aynesil.Application.Features.Meetings.Dtos;
using Aynesil.Application.Features.Meetings.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Meeting management: scheduling, participants, attendance, outcomes, follow-ups, calendar.
/// Route: /api/meetings
/// </summary>
[Route("api/meetings")]
public sealed class MeetingsController : BaseController
{
    // ── Meetings ──────────────────────────────────────────────────────────────────

    /// <summary>List meetings (paginated). Filterable by status, type, campus, organizer, date range.</summary>
    [HttpGet]
    [HasPermission(Permissions.Meetings.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<MeetingListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMeetings(
        [FromQuery] GetMeetingsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Get full meeting detail with participants, outcomes, and follow-ups.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Meetings.Read)]
    [ProducesResponseType(typeof(ApiResponse<MeetingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMeeting(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetMeetingQuery(id), ct));

    /// <summary>Schedule a new meeting. Participants may be included in the request body.</summary>
    [HttpPost]
    [HasPermission(Permissions.Meetings.Create)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleMeeting(
        [FromBody] ScheduleMeetingCommand command, CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedResult(id, $"/api/meetings/{id}");
    }

    /// <summary>Update meeting details (title, type, schedule, location, room, organizer).</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Meetings.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMeeting(
        Guid id, [FromBody] UpdateMeetingCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Soft-delete a meeting.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Meetings.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMeeting(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteMeetingCommand(id), ct);
        return NoContentResult();
    }

    // ── Status Transitions ────────────────────────────────────────────────────────

    /// <summary>Mark a meeting as completed (scheduled → completed).</summary>
    [HttpPost("{id:guid}/complete")]
    [HasPermission(Permissions.Meetings.Complete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteMeeting(Guid id, CancellationToken ct)
    {
        await Sender.Send(new CompleteMeetingCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Cancel a meeting (scheduled → cancelled).</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Meetings.Cancel)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelMeeting(Guid id, CancellationToken ct)
    {
        await Sender.Send(new CancelMeetingCommand(id), ct);
        return NoContentResult();
    }

    // ── Participants ──────────────────────────────────────────────────────────────

    /// <summary>Add a participant to a meeting.</summary>
    [HttpPost("{meetingId:guid}/participants")]
    [HasPermission(Permissions.Meetings.ManageParticipants)]
    [ProducesResponseType(typeof(ApiResponse<MeetingParticipantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddParticipant(
        Guid meetingId, [FromBody] AddMeetingParticipantCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { MeetingId = meetingId }, ct);
        return CreatedResult(result, $"/api/meetings/{meetingId}/participants/{result.Id}");
    }

    /// <summary>Update the attendance status for a participant.</summary>
    [HttpPatch("participants/{participantId:guid}/attendance")]
    [HasPermission(Permissions.Meetings.RecordAttendance)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAttendance(
        Guid participantId, [FromBody] UpdateAttendanceCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { ParticipantId = participantId }, ct);
        return NoContentResult();
    }

    /// <summary>Remove a participant from a meeting.</summary>
    [HttpDelete("participants/{participantId:guid}")]
    [HasPermission(Permissions.Meetings.ManageParticipants)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveParticipant(Guid participantId, CancellationToken ct)
    {
        await Sender.Send(new RemoveMeetingParticipantCommand(participantId), ct);
        return NoContentResult();
    }

    // ── Outcomes ──────────────────────────────────────────────────────────────────

    /// <summary>Record a new outcome (summary + decisions) for a completed meeting.</summary>
    [HttpPost("{meetingId:guid}/outcomes")]
    [HasPermission(Permissions.Meetings.RecordOutcome)]
    [ProducesResponseType(typeof(ApiResponse<MeetingOutcomeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordOutcome(
        Guid meetingId, [FromBody] RecordMeetingOutcomeCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { MeetingId = meetingId }, ct);
        return CreatedResult(result, $"/api/meetings/{meetingId}/outcomes/{result.Id}");
    }

    /// <summary>Update an existing meeting outcome.</summary>
    [HttpPut("outcomes/{outcomeId:guid}")]
    [HasPermission(Permissions.Meetings.RecordOutcome)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOutcome(
        Guid outcomeId, [FromBody] UpdateMeetingOutcomeCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = outcomeId }, ct);
        return NoContentResult();
    }

    // ── Follow-ups ────────────────────────────────────────────────────────────────

    /// <summary>Add a follow-up action item to a meeting.</summary>
    [HttpPost("{meetingId:guid}/follow-ups")]
    [HasPermission(Permissions.Meetings.ManageFollowUps)]
    [ProducesResponseType(typeof(ApiResponse<MeetingFollowUpDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFollowUp(
        Guid meetingId, [FromBody] AddMeetingFollowUpCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { MeetingId = meetingId }, ct);
        return CreatedResult(result, $"/api/meetings/{meetingId}/follow-ups/{result.Id}");
    }

    /// <summary>Update a follow-up item's action, assignee, or due date.</summary>
    [HttpPut("follow-ups/{followUpId:guid}")]
    [HasPermission(Permissions.Meetings.ManageFollowUps)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFollowUp(
        Guid followUpId, [FromBody] UpdateMeetingFollowUpCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = followUpId }, ct);
        return NoContentResult();
    }

    /// <summary>Update the status of a follow-up item (open → in_progress → done | cancelled).</summary>
    [HttpPatch("follow-ups/{followUpId:guid}/status")]
    [HasPermission(Permissions.Meetings.ManageFollowUps)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFollowUpStatus(
        Guid followUpId, [FromBody] UpdateFollowUpStatusCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = followUpId }, ct);
        return NoContentResult();
    }

    // ── Calendar ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calendar view: returns meetings within a date range.
    /// Supports all calendar contexts: school, campus, educator, student/parent.
    /// </summary>
    [HttpGet("calendar")]
    [HasPermission(Permissions.Meetings.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MeetingCalendarItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] GetMeetingCalendarQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));
}
