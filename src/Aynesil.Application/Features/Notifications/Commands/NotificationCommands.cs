using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Notifications.Commands;

// ── MarkNotificationReadCommand ───────────────────────────────────────────────

public record MarkNotificationReadCommand(Guid NotificationId, Guid RecipientUserId) : IRequest;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IAppDbContext _db;

    public MarkNotificationReadCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(MarkNotificationReadCommand req, CancellationToken ct)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == req.NotificationId
                                   && n.RecipientUserId == req.RecipientUserId, ct)
            ?? throw new KeyNotFoundException($"Notification {req.NotificationId} not found.");

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTimeOffset.UtcNow;
            notification.Status = "read";
            await _db.SaveChangesAsync(ct);
        }
    }
}

// ── MarkAllNotificationsReadCommand ──────────────────────────────────────────

public record MarkAllNotificationsReadCommand(Guid RecipientUserId) : IRequest<int>;

public sealed class MarkAllNotificationsReadCommandHandler
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly IAppDbContext _db;

    public MarkAllNotificationsReadCommandHandler(IAppDbContext db) => _db = db;

    public async Task<int> Handle(MarkAllNotificationsReadCommand req, CancellationToken ct)
    {
        var unread = await _db.Notifications
            .Where(n => n.RecipientUserId == req.RecipientUserId && n.ReadAt == null)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var n in unread)
        {
            n.ReadAt = now;
            n.Status = "read";
        }

        await _db.SaveChangesAsync(ct);
        return unread.Count;
    }
}

// ── SendNotificationCommand ────────────────────────────────────────────────────
// Workflow:
//  1. Optionally resolve template by trigger_code (if TriggerCode is provided).
//  2. Render subject/body using the resolved template (or raw subject/body).
//  3. Persist core.notification record.
//  4. Create core.notification_delivery rows for each channel in the trigger config.
//  5. Persist outbox_event for reliable async dispatch (Hangfire/background worker picks up).

public record SendNotificationCommand(
    Guid CorporationId,
    Guid RecipientUserId,
    /// <summary>If provided, trigger config + template are resolved automatically.</summary>
    string? TriggerCode,
    /// <summary>Template variables for body interpolation (JSON-serialisable).</summary>
    string Payload = "{}",
    /// <summary>Fallback subject when no template is found.</summary>
    string? SubjectOverride = null,
    /// <summary>Fallback body when no template is found.</summary>
    string? BodyOverride = null,
    Guid? CategoryId = null) : IRequest<Guid>;

public class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.RecipientUserId).NotEmpty();
        RuleFor(x => x).Must(x =>
            !string.IsNullOrWhiteSpace(x.TriggerCode) ||
            !string.IsNullOrWhiteSpace(x.BodyOverride))
            .WithMessage("Either TriggerCode or BodyOverride must be provided.");
    }
}

public sealed class SendNotificationCommandHandler
    : IRequestHandler<SendNotificationCommand, Guid>
{
    private readonly IAppDbContext _db;

    public SendNotificationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(SendNotificationCommand req, CancellationToken ct)
    {
        string? subject = req.SubjectOverride;
        string body = req.BodyOverride ?? string.Empty;
        Guid? templateId = null;
        Guid? categoryId = req.CategoryId;
        var deliveryChannelIds = new List<Guid>();

        // 1. Resolve trigger config and template (if trigger code provided)
        if (!string.IsNullOrWhiteSpace(req.TriggerCode))
        {
            var triggerConfig = await _db.NotificationTriggerConfigs
                .AsNoTracking()
                .Include(c => c.Channels)
                .Include(c => c.Template)
                    .ThenInclude(t => t!.Translations)
                .Where(c => c.TriggerCode == req.TriggerCode && c.IsActive
                         && (c.CorporationId == null || c.CorporationId == req.CorporationId))
                .OrderByDescending(c => c.CorporationId)  // tenant override wins over platform default
                .FirstOrDefaultAsync(ct);

            if (triggerConfig?.Template is not null)
            {
                templateId = triggerConfig.Template.Id;
                categoryId ??= triggerConfig.Template.CategoryId;

                // Prefer 'tr' locale, fallback to 'en', fallback to any
                var translation = triggerConfig.Template.Translations
                    .OrderBy(t => t.Locale == "tr" ? 0 : t.Locale == "en" ? 1 : 2)
                    .FirstOrDefault();

                if (translation is not null)
                {
                    subject = translation.Subject ?? subject;
                    body    = translation.Body;
                }
            }

            if (triggerConfig is not null)
                deliveryChannelIds.AddRange(triggerConfig.Channels.Select(ch => ch.ChannelId));
        }

        // Body is required — use override if template resolution produced nothing
        if (string.IsNullOrWhiteSpace(body))
            body = req.BodyOverride ?? throw new InvalidOperationException(
                "Could not resolve notification body from template or BodyOverride.");

        // 2. Persist notification
        var notification = new AppNotification
        {
            CorporationId   = req.CorporationId,
            TemplateId      = templateId,
            CategoryId      = categoryId,
            RecipientUserId = req.RecipientUserId,
            Subject         = subject,
            Body            = body,
            Payload         = req.Payload,
            Status          = "pending"
        };
        _db.Notifications.Add(notification);

        // 3. Create delivery records for each channel (or default in_app)
        if (!deliveryChannelIds.Any())
        {
            // Default: in_app delivery when no channels configured
            _db.NotificationDeliveries.Add(new NotificationDelivery
            {
                NotificationId = notification.Id,
                Status         = "queued"
            });
        }
        else
        {
            foreach (var channelId in deliveryChannelIds)
            {
                _db.NotificationDeliveries.Add(new NotificationDelivery
                {
                    NotificationId = notification.Id,
                    ChannelId      = channelId,
                    Status         = "queued"
                });
            }
        }

        // 4. Outbox event for reliable async dispatch
        _db.OutboxEvents.Add(new OutboxEvent
        {
            AggregateType = "Notification",
            AggregateId   = notification.Id,
            EventType     = "NotificationQueued",
            Payload       = System.Text.Json.JsonSerializer.Serialize(new
            {
                NotificationId  = notification.Id,
                CorporationId   = req.CorporationId,
                RecipientUserId = req.RecipientUserId,
                TriggerCode     = req.TriggerCode
            }),
            CorporationId = req.CorporationId
        });

        await _db.SaveChangesAsync(ct);
        return notification.Id;
    }
}
