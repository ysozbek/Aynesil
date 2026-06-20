namespace Aynesil.Application.Features.Users.Dtos;

/// <summary>Full user detail DTO. Returned by GetUser, CreateUser, UpdateUser, and Register.</summary>
public record UserDto(
    Guid Id,
    Guid CorporationId,
    string Username,
    string? Email,
    string? Phone,
    string FullName,
    string Status,
    string? PreferredLocale,
    Guid? PrimaryCampusId,
    bool MfaEnabled,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion
);
