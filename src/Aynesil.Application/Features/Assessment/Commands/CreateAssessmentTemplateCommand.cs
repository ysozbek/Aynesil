using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Domain.Modules.Assessment.Entities;
using FluentValidation;
using MediatR;

namespace Aynesil.Application.Features.Assessment.Commands;

// ── Request ───────────────────────────────────────────────────────────────────

public record CreateAssessmentTemplateCommand(
    Guid? CorporationId,
    string Code,
    string Name,
    Guid? TypeId,
    Guid? CategoryId,
    string? ScoringModel,
    IReadOnlyList<CreateTemplateTranslationRequest>? Translations) : IRequest<AssessmentTemplateDto>;

public record CreateTemplateTranslationRequest(string Locale, string Name, string? Description);

// ── Validator ─────────────────────────────────────────────────────────────────

public class CreateAssessmentTemplateCommandValidator : AbstractValidator<CreateAssessmentTemplateCommand>
{
    private static readonly string[] ValidScoringModels = ["sum", "average", "rubric", "none"];

    public CreateAssessmentTemplateCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ScoringModel)
            .Must(m => m is null || ValidScoringModels.Contains(m))
            .WithMessage("scoring_model must be one of: sum, average, rubric, none.");
        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Locale).NotEmpty().MaximumLength(20);
            t.RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        });
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateAssessmentTemplateCommandHandler
    : IRequestHandler<CreateAssessmentTemplateCommand, AssessmentTemplateDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateAssessmentTemplateCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentTemplateDto> Handle(
        CreateAssessmentTemplateCommand req, CancellationToken ct)
    {
        var template = AssessmentTemplate.Create(
            req.Code, req.Name,
            req.CorporationId, req.TypeId, req.CategoryId,
            req.ScoringModel, _currentUser.UserId);

        if (req.Translations is { Count: > 0 })
        {
            foreach (var tr in req.Translations)
                template.UpsertTranslation(tr.Locale, tr.Name, tr.Description);
        }

        _db.AssessmentTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return (await AssessmentProjection.LoadTemplateAsync(_db, template.Id, ct))!;
    }
}
