using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Application.Features.Scheduling.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Session &amp; Scheduling — Rooms, Calendar, Recurring Schedules, Sessions,
/// Attendance, Makeup Requests.
/// Route: /api/scheduling
/// </summary>
[Route("api/scheduling")]
public sealed class SchedulingController : BaseController
{
    // ══════════════════════════════════════════════════════════════════════════
    // ROOMS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("rooms")]
    [HasPermission(Permissions.Rooms.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<RoomListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRooms(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] bool? isVirtual = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetRoomsQuery
        {
            CorporationId = corporationId,
            CampusId      = campusId,
            IsVirtual     = isVirtual,
            IsActive      = isActive,
            Page          = page,
            PageSize      = pageSize,
            Search        = search,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("rooms/{id:guid}")]
    [HasPermission(Permissions.Rooms.Read)]
    [ProducesResponseType(typeof(ApiResponse<RoomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoom(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetRoomQuery(id), ct));

    [HttpPost("rooms")]
    [HasPermission(Permissions.Rooms.Create)]
    [ProducesResponseType(typeof(ApiResponse<RoomDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRoom(
        [FromBody] CreateRoomRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new CreateRoomCommand(
                req.CorporationId, req.Code, req.Name, req.Capacity,
                req.IsVirtual, req.CampusId, req.RoomTypeId, req.MeetingUrl), ct);
        return CreatedResult(result, $"/api/scheduling/rooms/{result.Id}");
    }

    [HttpPut("rooms/{id:guid}")]
    [HasPermission(Permissions.Rooms.Update)]
    [ProducesResponseType(typeof(ApiResponse<RoomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRoom(
        Guid id, [FromBody] UpdateRoomRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new UpdateRoomCommand(id, req.Code, req.Name, req.Capacity,
                req.RoomTypeId, req.MeetingUrl, req.RowVersion), ct));

    [HttpPost("rooms/{id:guid}/deactivate")]
    [HasPermission(Permissions.Rooms.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateRoom(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeactivateRoomCommand(id), ct);
        return NoContentResult("Room deactivated.");
    }

    [HttpDelete("rooms/{id:guid}")]
    [HasPermission(Permissions.Rooms.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRoom(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteRoomCommand(id), ct);
        return NoContentResult("Room deleted.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CALENDAR ENTRIES
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("calendar-entries")]
    [HasPermission(Permissions.Sessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CalendarEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalendarEntries(
        [FromQuery] Guid corporationId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? campusId = null,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetCalendarEntriesQuery(corporationId, from, to, campusId), ct));

    [HttpPost("calendar-entries")]
    [HasPermission(Permissions.Sessions.ManageCalendar)]
    [ProducesResponseType(typeof(ApiResponse<CalendarEntryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCalendarEntry(
        [FromBody] CreateCalendarEntryRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new CreateCalendarEntryCommand(
                req.CorporationId, req.Title, req.EntryType,
                req.StartsAt, req.EndsAt, req.IsAllDay, req.CampusId), ct);
        return CreatedResult(result, $"/api/scheduling/calendar-entries/{result.Id}");
    }

    [HttpDelete("calendar-entries/{id:guid}")]
    [HasPermission(Permissions.Sessions.ManageCalendar)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCalendarEntry(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteCalendarEntryCommand(id), ct);
        return NoContentResult("Calendar entry deleted.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // RECURRING SCHEDULES
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("recurring-schedules")]
    [HasPermission(Permissions.Sessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<RecurringScheduleListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecurringSchedules(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetRecurringSchedulesQuery
        {
            CorporationId = corporationId,
            CampusId      = campusId,
            IsActive      = isActive,
            Page          = page,
            PageSize      = pageSize,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("recurring-schedules/{id:guid}")]
    [HasPermission(Permissions.Sessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<RecurringScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecurringSchedule(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetRecurringScheduleQuery(id), ct));

    [HttpPost("recurring-schedules")]
    [HasPermission(Permissions.Sessions.Create)]
    [ProducesResponseType(typeof(ApiResponse<RecurringScheduleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRecurringSchedule(
        [FromBody] CreateRecurringScheduleRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new CreateRecurringScheduleCommand(
                req.CorporationId, req.Frequency, req.StartTime, req.DurationMinutes,
                req.RangeStart, req.CampusId, req.StudentProgramId, req.SessionTypeId,
                req.RoomId, req.IntervalCount, req.ByWeekday, req.ByMonthday,
                req.RangeEnd, req.MaxOccurrences), ct);
        return CreatedResult(result, $"/api/scheduling/recurring-schedules/{result.Id}");
    }

    [HttpPost("recurring-schedules/{id:guid}/deactivate")]
    [HasPermission(Permissions.Sessions.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateRecurringSchedule(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeactivateRecurringScheduleCommand(id), ct);
        return NoContentResult("Recurring schedule deactivated.");
    }

    [HttpPost("recurring-schedules/{id:guid}/exceptions")]
    [HasPermission(Permissions.Sessions.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddRecurrenceException(
        Guid id, [FromBody] AddRecurrenceExceptionRequest req, CancellationToken ct)
    {
        await Sender.Send(
            new AddRecurrenceExceptionCommand(id, req.ExceptionDate, req.Action,
                req.NewStartAt, req.Reason), ct);
        return NoContentResult("Recurrence exception added.");
    }

    [HttpPost("recurring-schedules/{id:guid}/generate")]
    [HasPermission(Permissions.Sessions.BulkGenerate)]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkGenerateSessions(
        Guid id, [FromBody] BulkGenerateRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new BulkGenerateSessionsCommand(id, req.WindowStart, req.WindowEnd), ct));

    [HttpPost("recurring-schedules/{id:guid}/bulk-cancel")]
    [HasPermission(Permissions.Sessions.BulkCancel)]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkCancelSessions(
        Guid id, [FromBody] BulkCancelRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new BulkCancelSessionsCommand(id, req.Reason, req.From, req.To), ct));

    [HttpPost("recurring-schedules/{id:guid}/bulk-reassign-room")]
    [HasPermission(Permissions.Sessions.BulkReassign)]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkReassignRoom(
        Guid id, [FromBody] BulkReassignRoomRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new BulkReassignRoomCommand(id, req.NewRoomId, req.From), ct));

    // ══════════════════════════════════════════════════════════════════════════
    // SESSIONS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("sessions")]
    [HasPermission(Permissions.Sessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<SessionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] Guid? roomId = null,
        [FromQuery] Guid? sessionTypeId = null,
        [FromQuery] Guid? recurringScheduleId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? isMakeup = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetSessionsQuery
        {
            CorporationId       = corporationId,
            CampusId            = campusId,
            RoomId              = roomId,
            SessionTypeId       = sessionTypeId,
            RecurringScheduleId = recurringScheduleId,
            Status              = status,
            IsMakeup            = isMakeup,
            From                = from,
            To                  = to,
            Page                = page,
            PageSize            = pageSize,
            Search              = search,
            SortBy              = sortBy,
            SortDirection       = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("sessions/{id:guid}")]
    [HasPermission(Permissions.Sessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetSessionQuery(id), ct));

    [HttpPost("sessions")]
    [HasPermission(Permissions.Sessions.Create)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> ScheduleSession(
        [FromBody] ScheduleSessionRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new ScheduleSessionCommand(
                req.CorporationId, req.SessionTypeId,
                req.StartsAt, req.EndsAt,
                req.CampusId, req.RoomId, req.RecurringScheduleId,
                req.ProgramServiceId, req.Title, req.IsMakeup,
                req.ParticipantStudentIds,
                req.EducatorAssignments), ct);
        return CreatedResult(result, $"/api/scheduling/sessions/{result.Id}");
    }

    [HttpPut("sessions/{id:guid}/reschedule")]
    [HasPermission(Permissions.Sessions.Reschedule)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RescheduleSession(
        Guid id, [FromBody] RescheduleSessionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new RescheduleSessionCommand(id, req.NewStartsAt, req.NewEndsAt, req.RoomId, req.RowVersion), ct));

    [HttpPost("sessions/{id:guid}/complete")]
    [HasPermission(Permissions.Sessions.Complete)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteSession(
        Guid id, [FromBody] RowVersionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new CompleteSessionCommand(id, req.RowVersion), ct));

    [HttpPost("sessions/{id:guid}/cancel")]
    [HasPermission(Permissions.Sessions.Cancel)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelSession(
        Guid id, [FromBody] CancelSessionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new CancelSessionCommand(id, req.Reason, req.RowVersion), ct));

    [HttpPost("sessions/{id:guid}/no-show")]
    [HasPermission(Permissions.Sessions.Cancel)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkNoShow(
        Guid id, [FromBody] RowVersionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new MarkSessionNoShowCommand(id, req.RowVersion), ct));

    [HttpDelete("sessions/{id:guid}")]
    [HasPermission(Permissions.Sessions.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSession(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteSessionCommand(id), ct);
        return NoContentResult("Session deleted.");
    }

    // ── Participants ──────────────────────────────────────────────────────────

    [HttpPost("sessions/{sessionId:guid}/participants")]
    [HasPermission(Permissions.Sessions.ManageParticipants)]
    [ProducesResponseType(typeof(ApiResponse<SessionParticipantDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddParticipant(
        Guid sessionId, [FromBody] AddParticipantRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new AddSessionParticipantCommand(
                sessionId, req.StudentId, req.StudentProgramId, req.Role), ct);
        return CreatedResult(result, $"/api/scheduling/sessions/{sessionId}");
    }

    [HttpDelete("sessions/{sessionId:guid}/participants/{studentId:guid}")]
    [HasPermission(Permissions.Sessions.ManageParticipants)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveParticipant(
        Guid sessionId, Guid studentId, CancellationToken ct)
    {
        await Sender.Send(new RemoveSessionParticipantCommand(sessionId, studentId), ct);
        return NoContentResult("Participant removed.");
    }

    // ── Educators ─────────────────────────────────────────────────────────────

    [HttpPost("sessions/{sessionId:guid}/educators")]
    [HasPermission(Permissions.Sessions.ManageEducators)]
    [ProducesResponseType(typeof(ApiResponse<SessionEducatorDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignEducator(
        Guid sessionId, [FromBody] AssignEducatorRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new AssignSessionEducatorCommand(sessionId, req.EducatorId, req.Role), ct);
        return CreatedResult(result, $"/api/scheduling/sessions/{sessionId}");
    }

    [HttpDelete("sessions/{sessionId:guid}/educators/{educatorId:guid}")]
    [HasPermission(Permissions.Sessions.ManageEducators)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveEducator(
        Guid sessionId, Guid educatorId, CancellationToken ct)
    {
        await Sender.Send(new RemoveSessionEducatorCommand(sessionId, educatorId), ct);
        return NoContentResult("Educator removed.");
    }

    // ── Goals ─────────────────────────────────────────────────────────────────

    [HttpPut("sessions/{sessionId:guid}/goals/{studentGoalId:guid}")]
    [HasPermission(Permissions.Sessions.ManageGoals)]
    [ProducesResponseType(typeof(ApiResponse<SessionGoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertSessionGoal(
        Guid sessionId, Guid studentGoalId,
        [FromBody] UpsertSessionGoalRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new UpsertSessionGoalCommand(
                sessionId, studentGoalId,
                req.WorkedOn, req.ProgressNote, req.MeasuredValue), ct));

    [HttpDelete("sessions/{sessionId:guid}/goals/{studentGoalId:guid}")]
    [HasPermission(Permissions.Sessions.ManageGoals)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveSessionGoal(
        Guid sessionId, Guid studentGoalId, CancellationToken ct)
    {
        await Sender.Send(new RemoveSessionGoalCommand(sessionId, studentGoalId), ct);
        return NoContentResult("Goal removed from session.");
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    [HttpPost("sessions/{sessionId:guid}/notes")]
    [HasPermission(Permissions.SessionNotes.Write)]
    [ProducesResponseType(typeof(ApiResponse<SessionNoteDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> WriteNote(
        Guid sessionId, [FromBody] WriteNoteRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new WriteSessionNoteCommand(sessionId, req.Body, req.ParentVisible), ct);
        return CreatedResult(result, $"/api/scheduling/sessions/{sessionId}");
    }

    [HttpPut("sessions/{sessionId:guid}/notes/{noteId:guid}")]
    [HasPermission(Permissions.SessionNotes.Write)]
    [ProducesResponseType(typeof(ApiResponse<SessionNoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EditNote(
        Guid sessionId, Guid noteId, [FromBody] EditNoteRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new EditSessionNoteCommand(noteId, req.Body, req.ParentVisible, req.RowVersion), ct));

    [HttpDelete("sessions/{sessionId:guid}/notes/{noteId:guid}")]
    [HasPermission(Permissions.SessionNotes.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteNote(
        Guid sessionId, Guid noteId, CancellationToken ct)
    {
        await Sender.Send(new DeleteSessionNoteCommand(noteId), ct);
        return NoContentResult("Note deleted.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ATTENDANCE
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("sessions/{sessionId:guid}/attendance")]
    [HasPermission(Permissions.Attendance.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AttendanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionAttendance(Guid sessionId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetSessionAttendanceQuery(sessionId), ct));

    [HttpPost("sessions/{sessionId:guid}/attendance")]
    [HasPermission(Permissions.Attendance.Record)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordAttendance(
        Guid sessionId, [FromBody] RecordAttendanceRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new RecordAttendanceCommand(
                sessionId, req.StudentId, req.Status,
                req.ReasonId, req.MinutesAttended, req.Note), ct));

    [HttpPost("sessions/{sessionId:guid}/attendance/bulk")]
    [HasPermission(Permissions.Attendance.Record)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AttendanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkRecordAttendance(
        Guid sessionId, [FromBody] BulkRecordAttendanceRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new BulkRecordAttendanceCommand(sessionId, req.Entries), ct));

    [HttpGet("students/{studentId:guid}/attendance")]
    [HasPermission(Permissions.Attendance.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AttendanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentAttendance(
        Guid studentId,
        [FromQuery] Guid corporationId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetStudentAttendanceQuery
        {
            CorporationId = corporationId,
            StudentId     = studentId,
            From          = from,
            To            = to,
            Status        = status,
            Page          = page,
            PageSize      = pageSize
        }, ct);
        return OkResult(result);
    }

    [HttpGet("students/{studentId:guid}/attendance/summary")]
    [HasPermission(Permissions.Attendance.Read)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceSummary(
        Guid studentId,
        [FromQuery] Guid corporationId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetAttendanceSummaryQuery(corporationId, studentId, from, to), ct));

    // ══════════════════════════════════════════════════════════════════════════
    // MAKEUP REQUESTS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("makeup-requests")]
    [HasPermission(Permissions.MakeupRequests.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<MakeupRequestListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMakeupRequests(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetMakeupRequestsQuery
        {
            CorporationId = corporationId,
            StudentId     = studentId,
            Status        = status,
            Page          = page,
            PageSize      = pageSize,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("makeup-requests/{id:guid}")]
    [HasPermission(Permissions.MakeupRequests.Read)]
    [ProducesResponseType(typeof(ApiResponse<MakeupRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMakeupRequest(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetMakeupRequestQuery(id), ct));

    [HttpPost("makeup-requests")]
    [HasPermission(Permissions.MakeupRequests.Request)]
    [ProducesResponseType(typeof(ApiResponse<MakeupRequestDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestMakeup(
        [FromBody] RequestMakeupRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new RequestMakeupCommand(
                req.CorporationId, req.StudentId,
                req.MissedSessionId, req.MissedReasonId,
                req.Note, req.ExpiresOn), ct);
        return CreatedResult(result, $"/api/scheduling/makeup-requests/{result.Id}");
    }

    [HttpPost("makeup-requests/{id:guid}/approve")]
    [HasPermission(Permissions.MakeupRequests.Manage)]
    [ProducesResponseType(typeof(ApiResponse<MakeupRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveMakeup(
        Guid id, [FromBody] RowVersionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new ApproveMakeupCommand(id, req.RowVersion), ct));

    [HttpPost("makeup-requests/{id:guid}/reject")]
    [HasPermission(Permissions.MakeupRequests.Manage)]
    [ProducesResponseType(typeof(ApiResponse<MakeupRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectMakeup(
        Guid id, [FromBody] RejectMakeupRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new RejectMakeupCommand(id, req.Note, req.RowVersion), ct));

    [HttpPost("makeup-requests/{id:guid}/assign-session")]
    [HasPermission(Permissions.MakeupRequests.Manage)]
    [ProducesResponseType(typeof(ApiResponse<MakeupRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignMakeupSession(
        Guid id, [FromBody] AssignMakeupSessionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(
            new AssignMakeupSessionCommand(id, req.MakeupSessionId, req.RowVersion), ct));

    [HttpPost("makeup-requests/{id:guid}/complete")]
    [HasPermission(Permissions.MakeupRequests.Manage)]
    [ProducesResponseType(typeof(ApiResponse<MakeupRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteMakeup(
        Guid id, [FromBody] RowVersionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new CompleteMakeupCommand(id, req.RowVersion), ct));

    // ══════════════════════════════════════════════════════════════════════════
    // CALENDAR VIEWS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("calendar/school")]
    [HasPermission(Permissions.Sessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<CalendarViewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchoolCalendar(
        [FromQuery] Guid corporationId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetSchoolCalendarQuery(corporationId, from, to), ct));

    [HttpGet("calendar/campus/{campusId:guid}")]
    [HasPermission(Permissions.Sessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<CalendarViewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampusCalendar(
        Guid campusId,
        [FromQuery] Guid corporationId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetCampusCalendarQuery(corporationId, campusId, from, to), ct));

    [HttpGet("calendar/room/{roomId:guid}")]
    [HasPermission(Permissions.Rooms.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SessionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoomCalendar(
        Guid roomId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(new GetRoomCalendarQuery(roomId, from, to), ct));

    [HttpGet("calendar/educator/{educatorId:guid}")]
    [HasPermission(Permissions.Sessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SessionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEducatorCalendar(
        Guid educatorId,
        [FromQuery] Guid corporationId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetEducatorCalendarQuery(corporationId, educatorId, from, to), ct));

    [HttpGet("calendar/student/{studentId:guid}")]
    [HasPermission(Permissions.Sessions.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SessionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentCalendar(
        Guid studentId,
        [FromQuery] Guid corporationId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(
            new GetStudentCalendarQuery(corporationId, studentId, from, to), ct));
}

// ── Request Records ───────────────────────────────────────────────────────────

// Rooms
public record CreateRoomRequest(
    Guid CorporationId,
    string Code,
    string Name,
    int Capacity,
    bool IsVirtual,
    Guid? CampusId,
    Guid? RoomTypeId,
    string? MeetingUrl);

public record UpdateRoomRequest(
    string Code,
    string Name,
    int Capacity,
    Guid? RoomTypeId,
    string? MeetingUrl,
    int RowVersion);

// Calendar Entries
public record CreateCalendarEntryRequest(
    Guid CorporationId,
    string Title,
    string EntryType,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsAllDay,
    Guid? CampusId);

// Recurring Schedules
public record CreateRecurringScheduleRequest(
    Guid CorporationId,
    string Frequency,
    TimeOnly StartTime,
    int DurationMinutes,
    DateOnly RangeStart,
    Guid? CampusId,
    Guid? StudentProgramId,
    Guid? SessionTypeId,
    Guid? RoomId,
    int IntervalCount,
    int[]? ByWeekday,
    int[]? ByMonthday,
    DateOnly? RangeEnd,
    int? MaxOccurrences);

public record AddRecurrenceExceptionRequest(
    DateOnly ExceptionDate,
    string Action,
    DateTimeOffset? NewStartAt,
    string? Reason);

public record BulkGenerateRequest(DateOnly WindowStart, DateOnly WindowEnd);
public record BulkCancelRequest(string? Reason, DateOnly? From, DateOnly? To);
public record BulkReassignRoomRequest(Guid? NewRoomId, DateOnly From);

// Sessions
public record ScheduleSessionRequest(
    Guid CorporationId,
    Guid SessionTypeId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid? CampusId,
    Guid? RoomId,
    Guid? RecurringScheduleId,
    Guid? ProgramServiceId,
    string? Title,
    bool IsMakeup,
    List<Guid> ParticipantStudentIds,
    List<EducatorAssignment> EducatorAssignments);

public record RescheduleSessionRequest(
    DateTimeOffset NewStartsAt,
    DateTimeOffset NewEndsAt,
    Guid? RoomId,
    int RowVersion);

public record CancelSessionRequest(string? Reason, int RowVersion);

public record AddParticipantRequest(
    Guid StudentId,
    Guid? StudentProgramId,
    string Role = "student");

public record AssignEducatorRequest(Guid EducatorId, string Role = "lead");

public record UpsertSessionGoalRequest(
    bool WorkedOn,
    string? ProgressNote,
    decimal? MeasuredValue);

public record WriteNoteRequest(string Body, bool ParentVisible);
public record EditNoteRequest(string Body, bool ParentVisible, int RowVersion);

// Attendance
public record RecordAttendanceRequest(
    Guid StudentId,
    string Status,
    Guid? ReasonId,
    int? MinutesAttended,
    string? Note);

public record BulkRecordAttendanceRequest(List<BulkAttendanceEntry> Entries);

// Makeup
public record RequestMakeupRequest(
    Guid CorporationId,
    Guid StudentId,
    Guid? MissedSessionId,
    Guid? MissedReasonId,
    string? Note,
    DateOnly? ExpiresOn);

public record RejectMakeupRequest(string? Note, int RowVersion);
public record AssignMakeupSessionRequest(Guid MakeupSessionId, int RowVersion);
