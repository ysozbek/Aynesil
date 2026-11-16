namespace Aynesil.Application.Features.CareTeam.Dtos;

/// <summary>Full detail of a single care-team assignment (used in GET single and command responses).</summary>
public record CareTeamAssignmentDto(
    Guid   Id,
    Guid   CorporationId,
    Guid   StudentId,
    Guid   EducatorId,
    /// <summary>Display name resolved from educator entity at query time.</summary>
    string EducatorName,
    Guid?  CampusId,
    Guid   RoleId,
    /// <summary>Code from ref.ref_value (care_team_role), e.g. "primary_therapist".</summary>
    string? RoleCode,
    bool   IsPrimary,
    string Status,
    DateOnly ActiveFrom,
    DateOnly? ActiveTo,
    Guid?  GrantTypeId,
    string? GrantTypeCode,
    Guid?  SourceAssignmentId,
    Guid?  GrantedBy,
    string? Reason,
    DateTimeOffset CreatedAt,
    int    RowVersion);

/// <summary>Compact summary used in list responses.</summary>
public record CareTeamAssignmentListItemDto(
    Guid   Id,
    Guid   StudentId,
    Guid   EducatorId,
    string EducatorName,
    Guid   RoleId,
    string? RoleCode,
    bool   IsPrimary,
    string Status,
    DateOnly ActiveFrom,
    DateOnly? ActiveTo,
    DateTimeOffset CreatedAt,
    /// <summary>Required by PUT/DELETE (optimistic concurrency).</summary>
    int RowVersion = 0,
    string? Reason = null);

/// <summary>Compact student summary used in "my-students" response.</summary>
public record CareTeamStudentListItemDto(
    Guid   StudentId,
    string StudentFullName,
    Guid   AssignmentId,
    Guid   RoleId,
    string? RoleCode,
    bool   IsPrimary,
    string Status,
    DateOnly ActiveFrom,
    DateOnly? ActiveTo);


/// <summary>
/// Educator derived from a student's sessions (not a care-team assignment). Used by the student dashboard while the
/// care team is still empty. Cancelled and no-show sessions are ignored.
/// </summary>
public record StudentSessionEducatorDto(
    Guid   EducatorId,
    string EducatorName,
    int    SessionCount,
    DateTimeOffset LastSessionAt);
