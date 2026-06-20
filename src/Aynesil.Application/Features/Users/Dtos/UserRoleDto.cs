namespace Aynesil.Application.Features.Users.Dtos;

/// <summary>Represents a single role assignment on a user.</summary>
public record UserRoleDto(
    Guid Id,
    Guid RoleId,
    string RoleCode,
    string RoleName,
    Guid? CampusId,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    DateTimeOffset CreatedAt
);
