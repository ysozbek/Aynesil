using Aynesil.Domain.Modules.Iam.Entities;

namespace Aynesil.Application.Features.Users.Dtos;

/// <summary>Mapping extension methods for UserAccount → DTOs.</summary>
public static class UserMappings
{
    public static UserDto ToDto(this UserAccount u) =>
        new(u.Id, u.CorporationId, u.Username, u.Email, u.Phone,
            u.FullName, u.Status, u.PreferredLocale, u.PrimaryCampusId,
            u.MfaEnabled, u.LastLoginAt, u.CreatedAt, u.UpdatedAt, u.RowVersion);

    public static UserListItemDto ToListItemDto(this UserAccount u) =>
        new(u.Id, u.Username, u.Email, u.FullName, u.Status,
            u.PreferredLocale, u.PrimaryCampusId, u.LastLoginAt, u.CreatedAt);

    public static UserRoleDto ToDto(this UserRole ur) =>
        new(ur.Id, ur.RoleId,
            ur.Role?.Code ?? string.Empty,
            ur.Role?.Name ?? string.Empty,
            ur.CampusId, ur.ValidFrom, ur.ValidTo, ur.CreatedAt);
}
