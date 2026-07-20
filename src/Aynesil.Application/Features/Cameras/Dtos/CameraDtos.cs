namespace Aynesil.Application.Features.Cameras.Dtos;

// ── Camera DTOs ───────────────────────────────────────────────────────────────

public record CameraListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    string? CampusName,
    Guid? CameraTypeId,
    string? CameraTypeCode,
    string Code,
    string Name,
    Guid? StreamProviderId,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record CameraDto(
    Guid Id,
    Guid CorporationId,
    Guid? CampusId,
    string? CampusName,
    Guid? CameraTypeId,
    string? CameraTypeCode,
    string Code,
    string Name,
    Guid? StreamProviderId,
    string? StreamRef,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<RoomCameraDto> RoomAssignments,
    IReadOnlyList<SessionCameraDto> SessionAssignments);

// ── Assignment DTOs ───────────────────────────────────────────────────────────

public record RoomCameraDto(
    Guid Id,
    Guid RoomId,
    string? RoomCode,
    string? RoomName,
    Guid CameraId,
    string CameraCode);

public record SessionCameraDto(
    Guid Id,
    Guid SessionId,
    DateTimeOffset SessionStartsAt,
    DateTimeOffset SessionEndsAt,
    Guid CameraId,
    string CameraCode);

// ── Viewing Authorization DTOs ────────────────────────────────────────────────

public record ViewingAuthorizationDto(
    Guid Id,
    Guid CorporationId,
    Guid GuardianId,
    string? GuardianFullName,
    Guid StudentId,
    string? StudentFullName,
    Guid? SessionId,
    Guid? ConsentId,
    Guid? AccessTypeId,
    string? AccessTypeCode,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    Guid? GrantedBy,
    bool IsRevoked,
    bool IsCurrentlyValid,
    DateTimeOffset CreatedAt);

// ── Viewing Log DTOs ──────────────────────────────────────────────────────────

public record ViewingLogDto(
    long Id,
    Guid CorporationId,
    Guid? GuardianId,
    string? GuardianFullName,
    Guid? UserId,
    Guid? SessionId,
    Guid? CameraId,
    string? CameraCode,
    Guid? AuthorizationId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int? DurationSeconds,
    string? IpAddress);
