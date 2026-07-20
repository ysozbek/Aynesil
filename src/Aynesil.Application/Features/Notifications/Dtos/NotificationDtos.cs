namespace Aynesil.Application.Features.Notifications.Dtos;

// ── Notification DTOs ─────────────────────────────────────────────────────────

public record NotificationDto(
    Guid Id,
    Guid CorporationId,
    Guid? TemplateId,
    Guid? CategoryId,
    string? CategoryCode,
    Guid? RecipientUserId,
    string? Subject,
    string Body,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    bool IsRead);

public record NotificationListItemDto(
    Guid Id,
    Guid? CategoryId,
    string? CategoryCode,
    string? Subject,
    string Body,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    bool IsRead);

public record UnreadCountDto(int Count);

// ── Template DTOs ─────────────────────────────────────────────────────────────

public record NotificationTemplateDto(
    Guid Id,
    Guid? CorporationId,
    string Code,
    Guid? CategoryId,
    string? CategoryCode,
    Guid? TypeId,
    string? TypeCode,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<NotificationTemplateTranslationDto> Translations);

public record NotificationTemplateListItemDto(
    Guid Id,
    Guid? CorporationId,
    string Code,
    string? CategoryCode,
    string? TypeCode,
    bool IsActive,
    DateTimeOffset UpdatedAt);

public record NotificationTemplateTranslationDto(
    string Locale,
    string? Subject,
    string Body);

// ── Trigger Config DTOs ───────────────────────────────────────────────────────

public record NotificationTriggerConfigDto(
    Guid Id,
    Guid? CorporationId,
    string TriggerCode,
    Guid? TemplateId,
    string? TemplateCode,
    int OffsetMinutes,
    bool IsActive,
    IReadOnlyList<Guid> ChannelIds,
    DateTimeOffset UpdatedAt,
    int RowVersion);

public record NotificationTriggerConfigListItemDto(
    Guid Id,
    Guid? CorporationId,
    string TriggerCode,
    string? TemplateCode,
    int OffsetMinutes,
    bool IsActive,
    int ChannelCount);

// ── Preference DTOs ───────────────────────────────────────────────────────────

public record NotificationPreferenceDto(
    Guid Id,
    Guid UserId,
    Guid? CategoryId,
    string? CategoryCode,
    Guid? ChannelId,
    string? ChannelCode,
    bool IsEnabled);
