using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Notifications.Dtos;
using Aynesil.Domain.Modules.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Notifications.Commands;

// ── UpsertNotificationTriggerConfigCommand ────────────────────────────────────
// Create or update a trigger config for (corporation_id, trigger_code).
// ChannelIds: list of ref_value(notification_channel) IDs — replaces existing channels.

public record UpsertNotificationTriggerConfigCommand(
    Guid? CorporationId,
    string TriggerCode,
    Guid? TemplateId,
    int OffsetMinutes,
    bool IsActive,
    IReadOnlyList<Guid> ChannelIds,
    Guid? CreatedBy = null) : IRequest<NotificationTriggerConfigDto>;

public class UpsertNotificationTriggerConfigCommandValidator
    : AbstractValidator<UpsertNotificationTriggerConfigCommand>
{
    public UpsertNotificationTriggerConfigCommandValidator()
    {
        RuleFor(x => x.TriggerCode).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpsertNotificationTriggerConfigCommandHandler
    : IRequestHandler<UpsertNotificationTriggerConfigCommand, NotificationTriggerConfigDto>
{
    private readonly IAppDbContext _db;

    public UpsertNotificationTriggerConfigCommandHandler(IAppDbContext db) => _db = db;

    public async Task<NotificationTriggerConfigDto> Handle(
        UpsertNotificationTriggerConfigCommand req, CancellationToken ct)
    {
        var config = await _db.NotificationTriggerConfigs
            .Include(c => c.Channels)
            .FirstOrDefaultAsync(c =>
                c.TriggerCode == req.TriggerCode &&
                c.CorporationId == req.CorporationId, ct);

        if (config is null)
        {
            config = NotificationTriggerConfig.Create(
                req.TriggerCode, req.TemplateId, req.OffsetMinutes,
                req.CorporationId, req.CreatedBy);
            _db.NotificationTriggerConfigs.Add(config);
        }
        else
        {
            config.Update(req.TemplateId, req.OffsetMinutes, req.IsActive);

            // Replace channels
            foreach (var ch in config.Channels.ToList())
                _db.NotificationTriggerChannels.Remove(ch);
        }

        foreach (var channelId in req.ChannelIds.Distinct())
        {
            _db.NotificationTriggerChannels.Add(
                NotificationTriggerChannel.Create(config.Id, channelId));
        }

        await _db.SaveChangesAsync(ct);

        return new NotificationTriggerConfigDto(
            config.Id, config.CorporationId, config.TriggerCode,
            config.TemplateId, null,
            config.OffsetMinutes, config.IsActive,
            req.ChannelIds.ToList(),
            config.UpdatedAt, config.RowVersion);
    }
}

// ── DeleteNotificationTriggerConfigCommand ────────────────────────────────────

public record DeleteNotificationTriggerConfigCommand(Guid Id) : IRequest;

public sealed class DeleteNotificationTriggerConfigCommandHandler
    : IRequestHandler<DeleteNotificationTriggerConfigCommand>
{
    private readonly IAppDbContext _db;

    public DeleteNotificationTriggerConfigCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteNotificationTriggerConfigCommand req, CancellationToken ct)
    {
        var config = await _db.NotificationTriggerConfigs
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"NotificationTriggerConfig {req.Id} not found.");

        _db.NotificationTriggerConfigs.Remove(config);
        await _db.SaveChangesAsync(ct);
    }
}

// ── UpdateNotificationPreferencesCommand ──────────────────────────────────────

public record UpdateNotificationPreferencesCommand(
    Guid CorporationId,
    Guid UserId,
    IReadOnlyList<PreferenceInput> Preferences) : IRequest;

public record PreferenceInput(Guid? CategoryId, Guid? ChannelId, bool IsEnabled);

public class UpdateNotificationPreferencesCommandValidator
    : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Preferences).NotNull();
    }
}

public sealed class UpdateNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateNotificationPreferencesCommand>
{
    private readonly IAppDbContext _db;

    public UpdateNotificationPreferencesCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateNotificationPreferencesCommand req, CancellationToken ct)
    {
        foreach (var pref in req.Preferences)
        {
            var existing = await _db.NotificationPreferences.FirstOrDefaultAsync(
                p => p.UserId == req.UserId
                  && p.CategoryId == pref.CategoryId
                  && p.ChannelId == pref.ChannelId, ct);

            if (existing is null)
            {
                _db.NotificationPreferences.Add(new NotificationPreference
                {
                    CorporationId = req.CorporationId,
                    UserId        = req.UserId,
                    CategoryId    = pref.CategoryId,
                    ChannelId     = pref.ChannelId,
                    IsEnabled     = pref.IsEnabled
                });
            }
            else
            {
                existing.IsEnabled = pref.IsEnabled;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
