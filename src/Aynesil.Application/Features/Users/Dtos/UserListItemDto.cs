namespace Aynesil.Application.Features.Users.Dtos;

/// <summary>Compact user projection for list screens. Excludes sensitive and rarely-needed fields.</summary>
public record UserListItemDto(
    Guid Id,
    string Username,
    string? Email,
    string FullName,
    string Status,
    string? PreferredLocale,
    Guid? PrimaryCampusId,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt
);
