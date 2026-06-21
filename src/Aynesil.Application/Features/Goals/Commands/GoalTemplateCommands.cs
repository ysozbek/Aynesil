using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Commands;

// ── CreateGoalTemplateCommand ─────────────────────────────────────────────────

public record CreateGoalTemplateCommand(
    Guid? CorporationId,
    Guid? LibraryId,
    Guid? CategoryId,
    Guid? DevelopmentAreaId,
    string? Code,
    string Statement,
    string? DefaultCriteria) : IRequest<GoalTemplateDto>;

public class CreateGoalTemplateCommandValidator : AbstractValidator<CreateGoalTemplateCommand>
{
    public CreateGoalTemplateCommandValidator()
    {
        RuleFor(x => x.Statement).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Code).MaximumLength(50).When(x => x.Code is not null);
    }
}

public sealed class CreateGoalTemplateCommandHandler
    : IRequestHandler<CreateGoalTemplateCommand, GoalTemplateDto>
{
    private readonly IAppDbContext _db;

    public CreateGoalTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task<GoalTemplateDto> Handle(CreateGoalTemplateCommand req, CancellationToken ct)
    {
        await ValidateRefsAsync(req.LibraryId, req.CategoryId, req.DevelopmentAreaId, ct);

        var template = GoalTemplate.Create(
            req.CorporationId, req.Statement, req.LibraryId,
            req.CategoryId, req.DevelopmentAreaId, req.Code, req.DefaultCriteria);

        _db.GoalTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return await LoadTemplateDtoAsync(template.Id, ct);
    }

    private async Task ValidateRefsAsync(
        Guid? libraryId, Guid? categoryId, Guid? devAreaId, CancellationToken ct)
    {
        if (libraryId.HasValue)
        {
            var libraryExists = await _db.GoalLibraries.AnyAsync(l => l.Id == libraryId.Value, ct);
            if (!libraryExists)
                throw new KeyNotFoundException($"GoalLibrary {libraryId} not found.");
        }

        if (categoryId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == categoryId.Value && r.RefType!.Code == "goal_category", ct);
            if (!valid)
                throw new KeyNotFoundException($"Invalid goal_category ref_value: {categoryId}");
        }

        if (devAreaId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == devAreaId.Value && r.RefType!.Code == "development_area", ct);
            if (!valid)
                throw new KeyNotFoundException($"Invalid development_area ref_value: {devAreaId}");
        }
    }

    private async Task<GoalTemplateDto> LoadTemplateDtoAsync(Guid id, CancellationToken ct)
    {
        var t = await _db.GoalTemplates
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();

        var catLabel = t.CategoryId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.CategoryId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var devAreaLabel = t.DevelopmentAreaId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == t.DevelopmentAreaId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var libraryName = t.LibraryId.HasValue
            ? await _db.GoalLibraries.AsNoTracking()
                .Where(l => l.Id == t.LibraryId.Value).Select(l => l.Name).FirstOrDefaultAsync(ct)
            : null;

        return new GoalTemplateDto(
            t.Id, t.CorporationId, t.LibraryId, libraryName,
            t.CategoryId, catLabel, t.DevelopmentAreaId, devAreaLabel,
            t.Code, t.Statement, t.DefaultCriteria,
            t.CreatedAt, t.UpdatedAt, t.RowVersion,
            t.Translations.Select(GoalProjection.ToTranslationDto).ToList());
    }
}

// ── UpdateGoalTemplateCommand ─────────────────────────────────────────────────

public record UpdateGoalTemplateCommand(
    Guid Id,
    Guid? LibraryId,
    Guid? CategoryId,
    Guid? DevelopmentAreaId,
    string? Code,
    string Statement,
    string? DefaultCriteria,
    int RowVersion) : IRequest<GoalTemplateDto>;

public class UpdateGoalTemplateCommandValidator : AbstractValidator<UpdateGoalTemplateCommand>
{
    public UpdateGoalTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Statement).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Code).MaximumLength(50).When(x => x.Code is not null);
    }
}

public sealed class UpdateGoalTemplateCommandHandler
    : IRequestHandler<UpdateGoalTemplateCommand, GoalTemplateDto>
{
    private readonly IAppDbContext _db;

    public UpdateGoalTemplateCommandHandler(IAppDbContext db) => _db = db;

    public async Task<GoalTemplateDto> Handle(UpdateGoalTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.GoalTemplates
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"GoalTemplate {req.Id} not found.");

        if (template.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "The template was modified by another user. Please refresh and retry.");

        template.Update(req.LibraryId, req.CategoryId, req.DevelopmentAreaId,
            req.Code, req.Statement, req.DefaultCriteria);

        await _db.SaveChangesAsync(ct);

        var catLabel = req.CategoryId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.CategoryId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var devAreaLabel = req.DevelopmentAreaId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.DevelopmentAreaId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var libraryName = req.LibraryId.HasValue
            ? await _db.GoalLibraries.AsNoTracking()
                .Where(l => l.Id == req.LibraryId.Value).Select(l => l.Name).FirstOrDefaultAsync(ct)
            : null;

        return new GoalTemplateDto(
            template.Id, template.CorporationId, template.LibraryId, libraryName,
            template.CategoryId, catLabel, template.DevelopmentAreaId, devAreaLabel,
            template.Code, template.Statement, template.DefaultCriteria,
            template.CreatedAt, template.UpdatedAt, template.RowVersion,
            template.Translations.Select(GoalProjection.ToTranslationDto).ToList());
    }
}

// ── SetGoalTemplateTranslationCommand ─────────────────────────────────────────

public record SetGoalTemplateTranslationCommand(
    Guid TemplateId,
    string Locale,
    string Statement,
    string? DefaultCriteria) : IRequest<GoalTemplateTranslationDto>;

public class SetGoalTemplateTranslationCommandValidator
    : AbstractValidator<SetGoalTemplateTranslationCommand>
{
    public SetGoalTemplateTranslationCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Statement).NotEmpty().MaximumLength(1000);
    }
}

public sealed class SetGoalTemplateTranslationCommandHandler
    : IRequestHandler<SetGoalTemplateTranslationCommand, GoalTemplateTranslationDto>
{
    private readonly IAppDbContext _db;

    public SetGoalTemplateTranslationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<GoalTemplateTranslationDto> Handle(
        SetGoalTemplateTranslationCommand req, CancellationToken ct)
    {
        var localeExists = await _db.Locales.AnyAsync(l => l.Code == req.Locale, ct);
        if (!localeExists)
            throw new KeyNotFoundException($"Locale '{req.Locale}' not found.");

        var template = await _db.GoalTemplates
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(t => t.Id == req.TemplateId, ct)
            ?? throw new KeyNotFoundException($"GoalTemplate {req.TemplateId} not found.");

        var translation = template.SetTranslation(req.Locale, req.Statement, req.DefaultCriteria);

        await _db.SaveChangesAsync(ct);

        return GoalProjection.ToTranslationDto(translation);
    }
}

// ── DeleteGoalTemplateCommand ─────────────────────────────────────────────────

public record DeleteGoalTemplateCommand(Guid Id) : IRequest;

public sealed class DeleteGoalTemplateCommandHandler : IRequestHandler<DeleteGoalTemplateCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteGoalTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteGoalTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.GoalTemplates.FirstOrDefaultAsync(t => t.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"GoalTemplate {req.Id} not found.");

        template.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
