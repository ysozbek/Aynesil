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
    DateTimeOffset CreatedAt);

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
