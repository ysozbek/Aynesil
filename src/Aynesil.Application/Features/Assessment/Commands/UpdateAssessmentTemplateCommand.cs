using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Commands;

// ── Update metadata ───────────────────────────────────────────────────────────

public record UpdateAssessmentTemplateCommand(
    Guid Id,
    string Name,
    Guid? TypeId,
    Guid? CategoryId,
    string? ScoringModel,
    int RowVersion) : IRequest<AssessmentTemplateDto>;

public class UpdateAssessmentTemplateCommandValidator : AbstractValidator<UpdateAssessmentTemplateCommand>
{
    private static readonly string[] ValidScoringModels = ["sum", "average", "rubric", "none"];

    public UpdateAssessmentTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ScoringModel)
            .Must(m => m is null || ValidScoringModels.Contains(m))
            .WithMessage("scoring_model must be one of: sum, average, rubric, none.");
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class UpdateAssessmentTemplateCommandHandler
    : IRequestHandler<UpdateAssessmentTemplateCommand, AssessmentTemplateDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateAssessmentTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentTemplateDto> Handle(
        UpdateAssessmentTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.AssessmentTemplates
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment template {req.Id} not found.");

        template.Update(req.Name, req.TypeId, req.CategoryId, req.ScoringModel, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return (await AssessmentProjection.LoadTemplateAsync(_db, template.Id, ct))!;
    }
}

// ── Activate / Deactivate ─────────────────────────────────────────────────────

public record SetAssessmentTemplateActiveCommand(Guid Id, bool IsActive, int RowVersion)
    : IRequest<AssessmentTemplateDto>;

public sealed class SetAssessmentTemplateActiveCommandHandler
    : IRequestHandler<SetAssessmentTemplateActiveCommand, AssessmentTemplateDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetAssessmentTemplateActiveCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentTemplateDto> Handle(
        SetAssessmentTemplateActiveCommand req, CancellationToken ct)
    {
        var template = await _db.AssessmentTemplates
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment template {req.Id} not found.");

        if (req.IsActive) template.Activate(_currentUser.UserId);
        else              template.Deactivate(_currentUser.UserId);

        await _db.SaveChangesAsync(ct);
        return (await AssessmentProjection.LoadTemplateAsync(_db, template.Id, ct))!;
    }
}

// ── Create new version ────────────────────────────────────────────────────────

/// <summary>
/// Forks the template into a new version row (version + 1, is_active = true)
/// and deactivates the source version. Sections and items are NOT copied — the
/// caller must add them to the new version via AddAssessmentSectionCommand.
/// This preserves historical sessions that reference the old template_id/version.
/// </summary>
public record CreateAssessmentTemplateVersionCommand(Guid Id) : IRequest<AssessmentTemplateDto>;

public sealed class CreateAssessmentTemplateVersionCommandHandler
    : IRequestHandler<CreateAssessmentTemplateVersionCommand, AssessmentTemplateDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateAssessmentTemplateVersionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentTemplateDto> Handle(
        CreateAssessmentTemplateVersionCommand req, CancellationToken ct)
    {
        var source = await _db.AssessmentTemplates
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment template {req.Id} not found.");

        var newVersion = source.CreateNewVersion(_currentUser.UserId);
        _db.AssessmentTemplates.Add(newVersion);
        await _db.SaveChangesAsync(ct);

        return (await AssessmentProjection.LoadTemplateAsync(_db, newVersion.Id, ct))!;
    }
}

// ── Upsert translation ────────────────────────────────────────────────────────

public record UpsertTemplateTranslationCommand(
    Guid TemplateId, string Locale, string Name, string? Description)
    : IRequest;

public class UpsertTemplateTranslationCommandValidator : AbstractValidator<UpsertTemplateTranslationCommand>
{
    public UpsertTemplateTranslationCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
    }
}

public sealed class UpsertTemplateTranslationCommandHandler
    : IRequestHandler<UpsertTemplateTranslationCommand>
{
    private readonly IAppDbContext _db;

    public UpsertTemplateTranslationCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpsertTemplateTranslationCommand req, CancellationToken ct)
    {
        var template = await _db.AssessmentTemplates
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(t => t.Id == req.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Assessment template {req.TemplateId} not found.");

        template.UpsertTranslation(req.Locale, req.Name, req.Description);
        await _db.SaveChangesAsync(ct);
    }
}
