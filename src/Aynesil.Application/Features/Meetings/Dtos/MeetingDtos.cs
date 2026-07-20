namespace Aynesil.Application.Features.Meetings.Dtos;

// ── Meeting DTOs ──────────────────────────────────────────────────────────────

public record MeetingListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    Guid? MeetingTypeId,
    string? MeetingTypeCode,
    string Title,
    string? Location,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? EndsAt,
    string Status,
    Guid? OrganizerId,
    int ParticipantCount,
    DateTimeOffset UpdatedAt);

public record MeetingDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    Guid? MeetingTypeId,
    string? MeetingTypeCode,
    string Title,
    string? Location,
    Guid? RoomId,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? EndsAt,
    string Status,
    Guid? OrganizerId,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<MeetingParticipantDto> Participants,
    IReadOnlyList<MeetingOutcomeDto> Outcomes,
    IReadOnlyList<MeetingFollowUpDto> FollowUps);

// ── Participant DTOs ──────────────────────────────────────────────────────────

public record MeetingParticipantDto(
    Guid Id,
    Guid MeetingId,
    Guid CorporationId,
    string ParticipantType,
    Guid? UserId,
    Guid? GuardianId,
    Guid? LeadId,
    string? ExternalName,
    string? Attendance);

// ── Outcome DTOs ──────────────────────────────────────────────────────────────

public record MeetingOutcomeDto(
    Guid Id,
    Guid MeetingId,
    string? Summary,
    string? Decisions,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy);

// ── Follow-up DTOs ────────────────────────────────────────────────────────────

public record MeetingFollowUpDto(
    Guid Id,
    Guid MeetingId,
    string Action,
    Guid? AssigneeId,
    DateOnly? DueDate,
    string Status,
    DateTimeOffset CreatedAt);

// ── Calendar DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Lightweight meeting entry for calendar views. Supports school, campus,
/// educator, and student/guardian calendar contexts.
/// </summary>
public record MeetingCalendarItemDto(
    Guid Id,
    string Title,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? EndsAt,
    Guid? MeetingTypeId,
    string? MeetingTypeCode,
    string Status,
    Guid? CampusId,
    string? Location,
    Guid? OrganizerId,
    int ParticipantCount);
