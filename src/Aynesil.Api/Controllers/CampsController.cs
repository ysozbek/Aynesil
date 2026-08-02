using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Camps.Commands;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Application.Features.Camps.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Camp Management: definitions, periods, enrollment workflow, attendance, reports.
/// Route: /api/camps
/// </summary>
[Route("api/camps")]
public sealed class CampsController : BaseController
{
    // ── Camps ─────────────────────────────────────────────────────────────────────

    /// <summary>Paginated list of camps. Filterable by corporation, campus, type, active status.</summary>
    [HttpGet]
    [HasPermission(Permissions.Camps.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CampListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCamps(
        [FromQuery] GetCampsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full camp detail including all periods.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Camps.Read)]
    [ProducesResponseType(typeof(ApiResponse<CampDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCamp(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetCampQuery(id), ct));

    /// <summary>Create a new camp definition.</summary>
    [HttpPost]
    [HasPermission(Permissions.Camps.Create)]
    [ProducesResponseType(typeof(ApiResponse<CampDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCamp(
        [FromBody] CreateCampCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/camps/{result.Id}");
    }

    /// <summary>Update camp details (name, type, campus, description, location, capacity).</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Camps.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCamp(
        Guid id, [FromBody] UpdateCampCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Activate a camp (inactive → active).</summary>
    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.Camps.Activate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateCamp(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ActivateCampCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Deactivate a camp (active → inactive).</summary>
    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Camps.Activate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCamp(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeactivateCampCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Soft-delete a camp. Fails if active enrollments exist.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Camps.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCamp(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteCampCommand(id), ct);
        return NoContentResult();
    }

    // ── Camp Reports (analytics) ───────────────────────────────────────────────────

    /// <summary>Enrollment summary per period for a camp (capacity management report).</summary>
    [HttpGet("{id:guid}/enrollment-summary")]
    [HasPermission(Permissions.Camps.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CampEnrollmentSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrollmentSummary(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetCampEnrollmentSummaryQuery(id), ct));

    /// <summary>Camp-level performance report (enrollment, completion, attendance rates).</summary>
    [HttpGet("performance")]
    [HasPermission(Permissions.Camps.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CampPerformanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampPerformance(
        [FromQuery] GetCampPerformanceQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    // ── Camp Periods ──────────────────────────────────────────────────────────────

    /// <summary>List all periods for a camp.</summary>
    [HttpGet("{campId:guid}/periods")]
    [HasPermission(Permissions.Camps.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CampPeriodListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPeriods(Guid campId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetCampPeriodsQuery(campId), ct));

    /// <summary>Get a single camp period by ID.</summary>
    [HttpGet("periods/{periodId:guid}")]
    [HasPermission(Permissions.Camps.Read)]
    [ProducesResponseType(typeof(ApiResponse<CampPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPeriod(Guid periodId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetCampPeriodQuery(periodId), ct));

    /// <summary>Add a new period to a camp.</summary>
    [HttpPost("{campId:guid}/periods")]
    [HasPermission(Permissions.Camps.ManagePeriods)]
    [ProducesResponseType(typeof(ApiResponse<CampPeriodDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePeriod(
        Guid campId, [FromBody] CreateCampPeriodCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { CampId = campId }, ct);
        return CreatedResult(result, $"/api/camps/periods/{result.Id}");
    }

    /// <summary>Update a camp period (name, dates, capacity).</summary>
    [HttpPut("periods/{periodId:guid}")]
    [HasPermission(Permissions.Camps.ManagePeriods)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePeriod(
        Guid periodId, [FromBody] UpdateCampPeriodCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = periodId }, ct);
        return NoContentResult();
    }

    /// <summary>Delete a camp period. Fails if it has any enrollments.</summary>
    [HttpDelete("periods/{periodId:guid}")]
    [HasPermission(Permissions.Camps.ManagePeriods)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePeriod(Guid periodId, CancellationToken ct)
    {
        await Sender.Send(new DeleteCampPeriodCommand(periodId), ct);
        return NoContentResult();
    }

    // ── Enrollments ───────────────────────────────────────────────────────────────

    /// <summary>Paginated enrollment list. Filterable by period, student, status.</summary>
    [HttpGet("enrollments")]
    [HasPermission(Permissions.CampEnrollments.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CampEnrollmentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrollments(
        [FromQuery] GetCampEnrollmentsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Get enrollment detail including attendance counts.</summary>
    [HttpGet("enrollments/{enrollmentId:guid}")]
    [HasPermission(Permissions.CampEnrollments.Read)]
    [ProducesResponseType(typeof(ApiResponse<CampEnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollment(Guid enrollmentId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetCampEnrollmentQuery(enrollmentId), ct));

    /// <summary>Enroll a student in a camp period (status: enrolled or waitlist).</summary>
    [HttpPost("periods/{periodId:guid}/enrollments")]
    [HasPermission(Permissions.CampEnrollments.Enroll)]
    [ProducesResponseType(typeof(ApiResponse<CampEnrollmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnrollStudent(
        Guid periodId, [FromBody] EnrollStudentCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { CampPeriodId = periodId }, ct);
        return CreatedResult(result, $"/api/camps/enrollments/{result.Id}");
    }

    /// <summary>Move an enrollment to waitlist (enrolled → waitlist).</summary>
    [HttpPost("enrollments/{enrollmentId:guid}/waitlist")]
    [HasPermission(Permissions.CampEnrollments.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveToWaitlist(Guid enrollmentId, CancellationToken ct)
    {
        await Sender.Send(new MoveToWaitlistCommand(enrollmentId), ct);
        return NoContentResult();
    }

    /// <summary>Promote a waitlisted student to enrolled (waitlist → enrolled).</summary>
    [HttpPost("enrollments/{enrollmentId:guid}/promote")]
    [HasPermission(Permissions.CampEnrollments.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PromoteFromWaitlist(Guid enrollmentId, CancellationToken ct)
    {
        await Sender.Send(new PromoteFromWaitlistCommand(enrollmentId), ct);
        return NoContentResult();
    }

    /// <summary>Withdraw a student from a camp period.</summary>
    [HttpPost("enrollments/{enrollmentId:guid}/withdraw")]
    [HasPermission(Permissions.CampEnrollments.Withdraw)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawEnrollment(Guid enrollmentId, CancellationToken ct)
    {
        await Sender.Send(new WithdrawEnrollmentCommand(enrollmentId), ct);
        return NoContentResult();
    }

    /// <summary>Mark an enrollment as completed.</summary>
    [HttpPost("enrollments/{enrollmentId:guid}/complete")]
    [HasPermission(Permissions.CampEnrollments.Complete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteEnrollment(Guid enrollmentId, CancellationToken ct)
    {
        await Sender.Send(new CompleteEnrollmentCommand(enrollmentId), ct);
        return NoContentResult();
    }

    /// <summary>Bulk-complete all active enrollments in a period.</summary>
    [HttpPost("periods/{periodId:guid}/bulk-complete")]
    [HasPermission(Permissions.CampEnrollments.Complete)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkCompleteEnrollments(Guid periodId, CancellationToken ct)
        => OkResult(await Sender.Send(new BulkCompleteEnrollmentsCommand(periodId), ct));

    // ── Attendance ────────────────────────────────────────────────────────────────

    /// <summary>Paginated attendance records. Filterable by enrollment, date range, status.</summary>
    [HttpGet("attendance")]
    [HasPermission(Permissions.CampAttendance.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CampAttendanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] GetCampAttendanceQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Attendance summary (present/absent/late/excused) per student for a period.</summary>
    [HttpGet("periods/{periodId:guid}/attendance-summary")]
    [HasPermission(Permissions.CampAttendance.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CampAttendanceSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceSummary(Guid periodId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetCampAttendanceSummaryQuery(periodId), ct));

    /// <summary>Record attendance for a single enrolled student on a given date.</summary>
    [HttpPost("attendance")]
    [HasPermission(Permissions.CampAttendance.Record)]
    [ProducesResponseType(typeof(ApiResponse<CampAttendanceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordAttendance(
        [FromBody] RecordAttendanceCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/camps/attendance/{result.Id}");
    }

    /// <summary>Bulk-record attendance for all students in a period on a given date.</summary>
    [HttpPost("periods/{periodId:guid}/attendance/bulk")]
    [HasPermission(Permissions.CampAttendance.Record)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkRecordAttendance(
        Guid periodId, [FromBody] BulkRecordAttendanceCommand command, CancellationToken ct)
        => OkResult(await Sender.Send(command with { CampPeriodId = periodId }, ct));

    /// <summary>Correct an existing attendance record (status or reason).</summary>
    [HttpPatch("attendance/{attendanceId:guid}")]
    [HasPermission(Permissions.CampAttendance.Record)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAttendance(
        Guid attendanceId, [FromBody] UpdateAttendanceCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { AttendanceId = attendanceId }, ct);
        return NoContentResult();
    }

    // ── Camp Reports (per-student) ────────────────────────────────────────────────

    /// <summary>List all reports for a student enrollment.</summary>
    [HttpGet("enrollments/{enrollmentId:guid}/reports")]
    [HasPermission(Permissions.CampReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CampReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports(Guid enrollmentId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetCampReportsQuery(enrollmentId), ct));

    /// <summary>Create a camp end-of-camp report for a student (summary and/or file).</summary>
    [HttpPost("enrollments/{enrollmentId:guid}/reports")]
    [HasPermission(Permissions.CampReports.Create)]
    [ProducesResponseType(typeof(ApiResponse<CampReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReport(
        Guid enrollmentId, [FromBody] CreateCampReportCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { CampEnrollmentId = enrollmentId }, ct);
        return CreatedResult(result, $"/api/camps/enrollments/{enrollmentId}/reports/{result.Id}");
    }

    // ── Activities ────────────────────────────────────────────────────────────────

    /// <summary>Paginated list of camp activities. Filterable by period, type, active.</summary>
    [HttpGet("activities")]
    [HasPermission(Permissions.CampActivities.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CampActivityListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivities(
        [FromQuery] GetCampActivitiesQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Get a single camp activity by ID.</summary>
    [HttpGet("activities/{activityId:guid}")]
    [HasPermission(Permissions.CampActivities.Read)]
    [ProducesResponseType(typeof(ApiResponse<CampActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActivity(Guid activityId, CancellationToken ct)
        => OkResult(await Sender.Send(new GetCampActivityQuery(activityId), ct));

    /// <summary>Create a camp activity within a period.</summary>
    [HttpPost("periods/{periodId:guid}/activities")]
    [HasPermission(Permissions.CampActivities.Create)]
    [ProducesResponseType(typeof(ApiResponse<CampActivityDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateActivity(
        Guid periodId, [FromBody] CreateCampActivityCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { CampPeriodId = periodId }, ct);
        return CreatedResult(result, $"/api/camps/activities/{result.Id}");
    }

    /// <summary>Update a camp activity.</summary>
    [HttpPut("activities/{activityId:guid}")]
    [HasPermission(Permissions.CampActivities.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateActivity(
        Guid activityId, [FromBody] UpdateCampActivityCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = activityId }, ct);
        return NoContentResult();
    }

    /// <summary>Soft-delete a camp activity.</summary>
    [HttpDelete("activities/{activityId:guid}")]
    [HasPermission(Permissions.CampActivities.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteActivity(Guid activityId, CancellationToken ct)
    {
        await Sender.Send(new DeleteCampActivityCommand(activityId), ct);
        return NoContentResult();
    }

    // ── Educator Assignments ──────────────────────────────────────────────────────

    /// <summary>List educator assignments. Filterable by camp, period, activity, educator.</summary>
    [HttpGet("educators")]
    [HasPermission(Permissions.CampEducators.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CampEducatorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEducators(
        [FromQuery] GetCampEducatorsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Assign an educator to a camp (optionally scoped to period/activity).</summary>
    [HttpPost("{campId:guid}/educators")]
    [HasPermission(Permissions.CampEducators.Manage)]
    [ProducesResponseType(typeof(ApiResponse<CampEducatorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignEducator(
        Guid campId, [FromBody] AssignCampEducatorCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { CampId = campId }, ct);
        return CreatedResult(result, $"/api/camps/educators/{result.Id}");
    }

    /// <summary>Update an educator assignment role.</summary>
    [HttpPatch("educators/{assignmentId:guid}/role")]
    [HasPermission(Permissions.CampEducators.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEducatorRole(
        Guid assignmentId, [FromBody] UpdateCampEducatorRoleCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = assignmentId }, ct);
        return NoContentResult();
    }

    /// <summary>Remove an educator assignment.</summary>
    [HttpDelete("educators/{assignmentId:guid}")]
    [HasPermission(Permissions.CampEducators.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEducator(Guid assignmentId, CancellationToken ct)
    {
        await Sender.Send(new RemoveCampEducatorCommand(assignmentId), ct);
        return NoContentResult();
    }

    // ── Activity Participation ────────────────────────────────────────────────────

    /// <summary>Paginated participation list. Filterable by activity, enrollment, status.</summary>
    [HttpGet("participations")]
    [HasPermission(Permissions.CampParticipation.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CampActivityParticipationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParticipations(
        [FromQuery] GetActivityParticipationsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Register a student enrollment for an activity.</summary>
    [HttpPost("activities/{activityId:guid}/participations")]
    [HasPermission(Permissions.CampParticipation.Record)]
    [ProducesResponseType(typeof(ApiResponse<CampActivityParticipationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterParticipation(
        Guid activityId, [FromBody] RegisterParticipationCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { CampActivityId = activityId }, ct);
        return CreatedResult(result, $"/api/camps/participations/{result.Id}");
    }

    /// <summary>Bulk-register participation for multiple enrollments on one activity.</summary>
    [HttpPost("activities/{activityId:guid}/participations/bulk")]
    [HasPermission(Permissions.CampParticipation.Record)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkRegisterParticipation(
        Guid activityId, [FromBody] BulkRegisterParticipationCommand command, CancellationToken ct)
        => OkResult(await Sender.Send(command with { CampActivityId = activityId }, ct));

    /// <summary>Update participation status (registered → attended | absent | excused).</summary>
    [HttpPatch("participations/{participationId:guid}")]
    [HasPermission(Permissions.CampParticipation.Record)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateParticipation(
        Guid participationId, [FromBody] UpdateParticipationCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = participationId }, ct);
        return NoContentResult();
    }
}
