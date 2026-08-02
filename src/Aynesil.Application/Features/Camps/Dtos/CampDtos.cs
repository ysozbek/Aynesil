namespace Aynesil.Application.Features.Camps.Dtos;

// ── Camp DTOs ─────────────────────────────────────────────────────────────────

public record CampListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    Guid? CampTypeId,
    string? CampTypeCode,
    string Code,
    string Name,
    string? Location,
    int? Capacity,
    bool IsActive,
    int PeriodCount,
    DateTimeOffset UpdatedAt);

public record CampDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    Guid? CampTypeId,
    string? CampTypeCode,
    string Code,
    string Name,
    string? Description,
    string? Location,
    int? Capacity,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<CampPeriodListItemDto> Periods);

// ── Period DTOs ───────────────────────────────────────────────────────────────

public record CampPeriodListItemDto(
    Guid Id,
    Guid CampId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int? Capacity,
    int EnrolledCount,
    int WaitlistCount);

public record CampPeriodDto(
    Guid Id,
    Guid CampId,
    Guid CorporationId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int? Capacity,
    int EnrolledCount,
    int WaitlistCount);

// ── Enrollment DTOs ───────────────────────────────────────────────────────────

public record CampEnrollmentListItemDto(
    Guid Id,
    Guid CampPeriodId,
    Guid StudentId,
    Guid? StudentPackageId,
    string Status,
    DateTimeOffset EnrolledAt);

public record CampEnrollmentDto(
    Guid Id,
    Guid CorporationId,
    Guid CampPeriodId,
    Guid StudentId,
    Guid? StudentPackageId,
    string Status,
    DateTimeOffset EnrolledAt,
    int AttendanceCount,
    int PresentCount,
    int AbsentCount);

// ── Attendance DTOs ───────────────────────────────────────────────────────────

public record CampAttendanceDto(
    Guid Id,
    Guid CampEnrollmentId,
    DateOnly AttendanceDate,
    string Status,
    Guid? ReasonId,
    Guid? RecordedBy);

// ── Report DTOs ───────────────────────────────────────────────────────────────

public record CampReportDto(
    Guid Id,
    Guid CampEnrollmentId,
    string? Summary,
    Guid? FileId,
    Guid? AuthoredBy,
    DateTimeOffset CreatedAt);

// ── Analytics / Report DTOs ───────────────────────────────────────────────────

public record CampEnrollmentSummaryDto(
    Guid CampPeriodId,
    string PeriodName,
    DateOnly StartDate,
    DateOnly EndDate,
    int? Capacity,
    int TotalEnrolled,
    int TotalWaitlist,
    int TotalWithdrawn,
    int TotalCompleted);

public record CampAttendanceSummaryDto(
    Guid EnrollmentId,
    Guid StudentId,
    int TotalDays,
    int Present,
    int Absent,
    int Late,
    int Excused,
    double AttendanceRatePct);

public record CampPerformanceDto(
    Guid CampId,
    string CampCode,
    string CampName,
    int TotalPeriods,
    int TotalEnrolled,
    int TotalCompleted,
    int TotalWithdrawn,
    double CompletionRatePct,
    double OverallAttendanceRatePct);

// ── Activity DTOs ─────────────────────────────────────────────────────────────

public record CampActivityListItemDto(
    Guid Id,
    Guid CampPeriodId,
    Guid? ActivityTypeId,
    string? ActivityTypeCode,
    string Name,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Location,
    int? Capacity,
    bool IsActive,
    int ParticipationCount);

public record CampActivityDto(
    Guid Id,
    Guid CorporationId,
    Guid CampPeriodId,
    Guid? ActivityTypeId,
    string? ActivityTypeCode,
    string Name,
    string? Description,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Location,
    int? Capacity,
    Guid? SessionId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── Educator Assignment DTOs ──────────────────────────────────────────────────

public record CampEducatorDto(
    Guid Id,
    Guid CorporationId,
    Guid CampId,
    Guid? CampPeriodId,
    Guid? CampActivityId,
    Guid EducatorId,
    string Role,
    DateTimeOffset AssignedAt,
    Guid? AssignedBy);

// ── Participation DTOs ────────────────────────────────────────────────────────

public record CampActivityParticipationDto(
    Guid Id,
    Guid CorporationId,
    Guid CampActivityId,
    Guid CampEnrollmentId,
    string Status,
    string? Notes,
    Guid? RecordedBy,
    DateTimeOffset RecordedAt);
