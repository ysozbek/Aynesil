using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Notifications.Dtos;
using Aynesil.Application.Features.Notifications.Queries;
using Aynesil.Domain.Modules.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Notifications.Commands;

// ── CreateNotificationTemplateCommand ─────────────────────────────────────────

public record CreateNotificationTemplateCommand(
    Guid? CorporationId,
    string Code,
    Guid? CategoryId,
    Guid? TypeId,
    /// <summary>Locale → (Subject, Body) pairs. At least one required.</summary>
    IReadOnlyList<TranslationInput> Translations) : IRequest<NotificationTemplateDto>;

public record TranslationInput(string Locale, string? Subject, string Body);

public class CreateNotificationTemplateCommandValidator
    : AbstractValidator<CreateNotificationTemplateCommand>
{
    public CreateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Translations).NotEmpty()
            .WithMessage("At least one translation is required.");
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
            t.RuleFor(x => x.Body).NotEmpty();
        });
    }
}

public sealed class CreateNotificationTemplateCommandHandler
    : IRequestHandler<CreateNotificationTemplateCommand, NotificationTemplateDto>
{
    private readonly IAppDbContext _db;
    private readonly IMediator _mediator;

    public CreateNotificationTemplateCommandHandler(IAppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<NotificationTemplateDto> Handle(
        CreateNotificationTemplateCommand req, CancellationToken ct)
    {
        var existing = await _db.NotificationTemplates.AnyAsync(
            t => t.Code == req.Code && t.CorporationId == req.CorporationId, ct);
        if (existing)
            throw new InvalidOperationException(
                $"Notification template with code '{req.Code}' already exists for this corporation.");

        var template = new NotificationTemplate
        {
            CorporationId = req.CorporationId,
            Code          = req.Code.Trim().ToLowerInvariant(),
            CategoryId    = req.CategoryId,
            TypeId        = req.TypeId,
            IsActive      = true
        };
        _db.NotificationTemplates.Add(template);

        foreach (var t in req.Translations)
        {
            _db.NotificationTemplateTranslations.Add(new NotificationTemplateTranslation
            {
                TemplateId = template.Id,
                Locale     = t.Locale.Trim().ToLowerInvariant(),
                Subject    = t.Subject?.Trim(),
                Body       = t.Body.Trim()
            });
        }

        await _db.SaveChangesAsync(ct);
        return await _mediator.Send(new GetNotificationTemplateQuery(template.Id), ct);
    }
}

// ── UpdateNotificationTemplateCommand ─────────────────────────────────────────

public record UpdateNotificationTemplateCommand(
    Guid Id,
    Guid? CategoryId,
    Guid? TypeId,
    bool IsActive,
    IReadOnlyList<TranslationInput> Translations,
    int RowVersion) : IRequest<NotificationTemplateDto>;

public class UpdateNotificationTemplateCommandValidator
    : AbstractValidator<UpdateNotificationTemplateCommand>
{
    public UpdateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Translations).NotEmpty()
            .WithMessage("At least one translation is required.");
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
            t.RuleFor(x => x.Body).NotEmpty();
        });
    }
}

public sealed class UpdateNotificationTemplateCommandHandler
    : IRequestHandler<UpdateNotificationTemplateCommand, NotificationTemplateDto>
{
    private readonly IAppDbContext _db;
    private readonly IMediator _mediator;

    public UpdateNotificationTemplateCommandHandler(IAppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<NotificationTemplateDto> Handle(
        UpdateNotificationTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.NotificationTemplates
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"NotificationTemplate {req.Id} not found.");

        template.CategoryId = req.CategoryId;
        template.TypeId     = req.TypeId;
        template.IsActive   = req.IsActive;
        template.UpdatedAt  = DateTimeOffset.UtcNow;
        template.RowVersion = req.RowVersion;

        // Upsert translations — remove stale locales, add/update current
        var incoming = req.Translations.ToDictionary(t => t.Locale.Trim().ToLowerInvariant());
        var existing = template.Translations.ToDictionary(t => t.Locale);

        foreach (var locale in existing.Keys.Except(incoming.Keys).ToList())
            _db.NotificationTemplateTranslations.Remove(existing[locale]);

        foreach (var (locale, input) in incoming)
        {
            if (existing.TryGetValue(locale, out var tr))
            {
                tr.Subject = input.Subject?.Trim();
                tr.Body    = input.Body.Trim();
            }
            else
            {
                _db.NotificationTemplateTranslations.Add(new NotificationTemplateTranslation
                {
                    TemplateId = template.Id,
                    Locale     = locale,
                    Subject    = input.Subject?.Trim(),
                    Body       = input.Body.Trim()
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return await _mediator.Send(new GetNotificationTemplateQuery(template.Id), ct);
    }
}

// ── DeleteNotificationTemplateCommand ─────────────────────────────────────────

public record DeleteNotificationTemplateCommand(Guid Id) : IRequest;

public sealed class DeleteNotificationTemplateCommandHandler
    : IRequestHandler<DeleteNotificationTemplateCommand>
{
    private readonly IAppDbContext _db;

    public DeleteNotificationTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteNotificationTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"NotificationTemplate {req.Id} not found.");

        // Soft-deactivate: templates referenced by trigger configs cannot be hard-deleted
        var isReferenced = await _db.NotificationTriggerConfigs
            .AnyAsync(c => c.TemplateId == req.Id, ct);

        if (isReferenced)
        {
            template.IsActive  = false;
            template.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            _db.NotificationTemplates.Remove(template);
        }

        await _db.SaveChangesAsync(ct);
    }
}
