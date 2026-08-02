using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using Aynesil.Domain.Modules.Legal.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Commands;

// ── CreateConsentTemplateCommand ──────────────────────────────────────────────

public record ConsentTranslationRequest(string Locale, string Title, string Body);

public record CreateConsentTemplateCommand(
    Guid CorporationId,
    string Code,
    Guid? ConsentTypeId,
    bool IsMandatory,
    DateOnly? EffectiveFrom,
    IReadOnlyList<ConsentTranslationRequest>? Translations,
    Guid? CreatedBy = null) : IRequest<ConsentTemplateDto>;

public class CreateConsentTemplateCommandValidator : AbstractValidator<CreateConsentTemplateCommand>
{
    public CreateConsentTemplateCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z0-9_]+$").WithMessage("Code must be lowercase alphanumeric with underscores.");
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty().MaximumLength(20);
            t.RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            t.RuleFor(x => x.Body).NotEmpty();
        });
    }
}

public sealed class CreateConsentTemplateCommandHandler
    : IRequestHandler<CreateConsentTemplateCommand, ConsentTemplateDto>
{
    private readonly IAppDbContext _db;

    public CreateConsentTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ConsentTemplateDto> Handle(
        CreateConsentTemplateCommand req, CancellationToken ct)
    {
        var alreadyExists = await _db.ConsentTemplates
            .AnyAsync(t => t.CorporationId == req.CorporationId
                        && t.Code == req.Code.Trim().ToLowerInvariant()
                        && t.IsCurrent, ct);

        if (alreadyExists)
            throw new InvalidOperationException(
                $"A current consent template with code '{req.Code}' already exists for this corporation.");

        var template = ConsentTemplate.Create(
            req.CorporationId, req.Code, req.ConsentTypeId, req.IsMandatory, req.EffectiveFrom);

        if (req.Translations is { Count: > 0 })
            foreach (var tr in req.Translations)
                template.UpsertTranslation(tr.Locale, tr.Title, tr.Body);

        _db.ConsentTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(template.Id, ct);
    }

    private async Task<ConsentTemplateDto> BuildDtoAsync(Guid id, CancellationToken ct)
    {
        var t = await _db.ConsentTemplates
            .Include(x => x.Translations)
            .AsNoTracking()
            .FirstAsync(x => x.Id == id, ct);

        var typeCode = t.ConsentTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.ConsentTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return MapToDto(t, typeCode);
    }

    internal static ConsentTemplateDto MapToDto(ConsentTemplate t, string? typeCode) => new(
        t.Id, t.CorporationId, t.Code,
        t.ConsentTypeId, typeCode,
        t.Version, t.IsCurrent, t.IsMandatory, t.EffectiveFrom,
        t.Translations.Select(x => new ConsentTemplateTranslationDto(x.Locale, x.Title, x.Body)).ToList(),
        t.CreatedAt, t.UpdatedAt, t.RowVersion);
}

// ── UpdateConsentTemplateCommand ──────────────────────────────────────────────

public record UpdateConsentTemplateCommand(
    Guid Id,
    Guid? ConsentTypeId,
    bool IsMandatory,
    DateOnly? EffectiveFrom,
    IReadOnlyList<ConsentTranslationRequest>? Translations,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateConsentTemplateCommandValidator : AbstractValidator<UpdateConsentTemplateCommand>
{
    public UpdateConsentTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty().MaximumLength(20);
            t.RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            t.RuleFor(x => x.Body).NotEmpty();
        });
    }
}

public sealed class UpdateConsentTemplateCommandHandler : IRequestHandler<UpdateConsentTemplateCommand>
{
    private readonly IAppDbContext _db;

    public UpdateConsentTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateConsentTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.ConsentTemplates
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consent template {req.Id} not found.");

        template.Update(req.ConsentTypeId, req.IsMandatory, req.EffectiveFrom);

        if (req.Translations is { Count: > 0 })
            foreach (var tr in req.Translations)
                template.UpsertTranslation(tr.Locale, tr.Title, tr.Body);

        await _db.SaveChangesAsync(ct);
    }
}

// ── NewVersionConsentTemplateCommand ──────────────────────────────────────────

public record NewVersionConsentTemplateCommand(
    Guid CurrentTemplateId,
    DateOnly? EffectiveFrom,
    IReadOnlyList<ConsentTranslationRequest>? Translations,
    Guid? CreatedBy = null) : IRequest<ConsentTemplateDto>;

public class NewVersionConsentTemplateCommandValidator
    : AbstractValidator<NewVersionConsentTemplateCommand>
{
    public NewVersionConsentTemplateCommandValidator()
    {
        RuleFor(x => x.CurrentTemplateId).NotEmpty();
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty().MaximumLength(20);
            t.RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            t.RuleFor(x => x.Body).NotEmpty();
        });
    }
}

public sealed class NewVersionConsentTemplateCommandHandler
    : IRequestHandler<NewVersionConsentTemplateCommand, ConsentTemplateDto>
{
    private readonly IAppDbContext _db;

    public NewVersionConsentTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ConsentTemplateDto> Handle(
        NewVersionConsentTemplateCommand req, CancellationToken ct)
    {
        var current = await _db.ConsentTemplates
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == req.CurrentTemplateId, ct)
            ?? throw new KeyNotFoundException($"Consent template {req.CurrentTemplateId} not found.");

        var next = current.NewVersion(req.EffectiveFrom);

        if (req.Translations is { Count: > 0 })
        {
            foreach (var tr in req.Translations)
                next.UpsertTranslation(tr.Locale, tr.Title, tr.Body);
        }
        else
        {
            foreach (var tr in current.Translations)
                next.UpsertTranslation(tr.Locale, tr.Title, tr.Body);
        }

        _db.ConsentTemplates.Add(next);
        await _db.SaveChangesAsync(ct);

        var typeCode = next.ConsentTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == next.ConsentTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return CreateConsentTemplateCommandHandler.MapToDto(next, typeCode);
    }
}

// ── DeleteConsentTemplateCommand ──────────────────────────────────────────────

public record DeleteConsentTemplateCommand(Guid Id, Guid? DeletedBy = null) : IRequest;

public sealed class DeleteConsentTemplateCommandHandler : IRequestHandler<DeleteConsentTemplateCommand>
{
    private readonly IAppDbContext _db;

    public DeleteConsentTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteConsentTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.ConsentTemplates
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consent template {req.Id} not found.");

        var hasConsents = await _db.StudentConsents
            .AnyAsync(c => c.TemplateId == req.Id, ct);

        if (hasConsents)
            throw new InvalidOperationException(
                "Cannot delete a consent template that is referenced by student consent records.");

        template.Delete(req.DeletedBy);
        await _db.SaveChangesAsync(ct);
    }
}
