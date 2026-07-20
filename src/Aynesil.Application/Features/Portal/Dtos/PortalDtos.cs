namespace Aynesil.Application.Features.Portal.Dtos;

// ── Portal Dashboard ──────────────────────────────────────────────────────────

public record PortalDashboardDto(
    Guid StudentId,
    int? UpcomingSessions,
    int UnreadNotifications,
    decimal? PackageBalance,
    int? ActiveGoals);

// ── Portal Sessions ───────────────────────────────────────────────────────────

public record PortalSessionDto(
    Guid Id,
    string? Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status);

// ── Portal Attendance ─────────────────────────────────────────────────────────

public record PortalAttendanceDto(
    Guid SessionId,
    string? SessionTitle,
    DateTimeOffset SessionStartsAt,
    string AttendanceStatus,
    Guid? ReasonId);

// ── Portal Packages ───────────────────────────────────────────────────────────

public record PortalPackageDto(
    Guid Id,
    Guid StudentId,
    decimal TotalCredits,
    decimal RemainingCredits,
    DateOnly? ExpiresOn,
    string Status);

// ── Portal Documents ──────────────────────────────────────────────────────────

public record PortalDocumentDto(
    Guid FileId,
    string OriginalName,
    string? Purpose,
    string? MimeType,
    long? ByteSize,
    DateTimeOffset CreatedAt);

// ── Portal Education Plan (BEP) ───────────────────────────────────────────────

public record PortalEducationPlanDto(
    Guid Id,
    string? Title,
    int Version,
    string Status,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo);

// ── Portal Goal Progress ──────────────────────────────────────────────────────

public record PortalGoalProgressDto(
    Guid GoalId,
    string Statement,
    string? Horizon,
    string Status,
    decimal? PercentComplete,
    string? Trend,
    DateOnly? TargetDate);

// ── Portal Meeting History ────────────────────────────────────────────────────

public record PortalMeetingDto(
    Guid Id,
    string Title,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? EndsAt,
    string Status,
    string? Location,
    string? GuardianAttendance);
