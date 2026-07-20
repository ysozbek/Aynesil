using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Surveys.Dtos;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Surveys.Commands;

// ── CreateSurveyDefinitionCommand ─────────────────────────────────────────────

public record CreateSurveyDefinitionCommand(
    Guid CorporationId,
    string Title,
    string Target,
    Guid? TypeId,
    string? Description,
    IReadOnlyList<QuestionInput> Questions,
    Guid? CreatedBy = null) : IRequest<Guid>;

public record QuestionInput(
    string QuestionText,
    string QuestionType,
    bool IsRequired,
    int SortOrder,
    IReadOnlyList<AnswerOptionInput> AnswerOptions);

public record AnswerOptionInput(string OptionText, string? OptionValue, int SortOrder);

public class CreateSurveyDefinitionCommandValidator
    : AbstractValidator<CreateSurveyDefinitionCommand>
{
    private static readonly string[] ValidTargets = ["guardian", "educator", "student"];
    private static readonly string[] ValidQuestionTypes =
        ["text", "rating", "yes_no", "multiple_choice", "scale"];

    public CreateSurveyDefinitionCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Target).Must(t => ValidTargets.Contains(t))
            .WithMessage("Target must be 'guardian', 'educator', or 'student'.");
        RuleFor(x => x.Questions).NotEmpty()
            .WithMessage("A survey must have at least one question.");
        RuleForEach(x => x.Questions).ChildRules(q =>
        {
            q.RuleFor(x => x.QuestionText).NotEmpty();
            q.RuleFor(x => x.QuestionType)
                .Must(t => ValidQuestionTypes.Contains(t))
                .WithMessage("Invalid question type.");
        });
    }
}

public sealed class CreateSurveyDefinitionCommandHandler
    : IRequestHandler<CreateSurveyDefinitionCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateSurveyDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateSurveyDefinitionCommand req, CancellationToken ct)
    {
        var survey = SurveyDefinition.Create(
            req.CorporationId, req.Title, req.Target,
            req.TypeId, req.Description, req.CreatedBy);
        _db.SurveyDefinitions.Add(survey);

        foreach (var q in req.Questions.OrderBy(x => x.SortOrder))
        {
            var question = SurveyQuestion.Create(
                survey.Id, q.QuestionText, q.QuestionType, q.IsRequired, q.SortOrder, req.CreatedBy);
            _db.SurveyQuestions.Add(question);

            foreach (var opt in q.AnswerOptions.OrderBy(x => x.SortOrder))
            {
                _db.SurveyAnswerOptions.Add(
                    SurveyAnswerOption.Create(question.Id, opt.OptionText, opt.OptionValue, opt.SortOrder));
            }
        }

        await _db.SaveChangesAsync(ct);
        return survey.Id;
    }
}

// ── UpdateSurveyDefinitionCommand ─────────────────────────────────────────────

public record UpdateSurveyDefinitionCommand(
    Guid Id,
    string Title,
    string? Description,
    string Target,
    Guid? TypeId,
    bool IsActive,
    int RowVersion) : IRequest;

public class UpdateSurveyDefinitionCommandValidator
    : AbstractValidator<UpdateSurveyDefinitionCommand>
{
    public UpdateSurveyDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateSurveyDefinitionCommandHandler
    : IRequestHandler<UpdateSurveyDefinitionCommand>
{
    private readonly IAppDbContext _db;

    public UpdateSurveyDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateSurveyDefinitionCommand req, CancellationToken ct)
    {
        var survey = await _db.SurveyDefinitions
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"SurveyDefinition {req.Id} not found.");

        survey.Update(req.Title, req.Description, req.Target, req.TypeId, req.IsActive);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteSurveyDefinitionCommand ─────────────────────────────────────────────

public record DeleteSurveyDefinitionCommand(Guid Id) : IRequest;

public sealed class DeleteSurveyDefinitionCommandHandler
    : IRequestHandler<DeleteSurveyDefinitionCommand>
{
    private readonly IAppDbContext _db;

    public DeleteSurveyDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteSurveyDefinitionCommand req, CancellationToken ct)
    {
        var survey = await _db.SurveyDefinitions
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"SurveyDefinition {req.Id} not found.");

        var hasResponses = await _db.SurveyResponses.AnyAsync(r => r.SurveyId == req.Id, ct);
        if (hasResponses)
        {
            survey.Deactivate();
        }
        else
        {
            survey.SoftDelete();
        }
        await _db.SaveChangesAsync(ct);
    }
}

// ── SubmitSurveyResponseCommand ───────────────────────────────────────────────

public record SubmitSurveyResponseCommand(
    Guid CorporationId,
    Guid SurveyId,
    Guid? RespondentUserId,
    Guid? GuardianId,
    Guid? StudentId,
    Guid? SessionId,
    IReadOnlyList<QuestionAnswerInput> Answers) : IRequest<Guid>;

public record QuestionAnswerInput(
    Guid QuestionId,
    string? AnswerText,
    Guid? AnswerOptionId,
    decimal? NumericValue);

public class SubmitSurveyResponseCommandValidator
    : AbstractValidator<SubmitSurveyResponseCommand>
{
    public SubmitSurveyResponseCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.SurveyId).NotEmpty();
        RuleFor(x => x.Answers).NotEmpty()
            .WithMessage("At least one answer must be provided.");
    }
}

public sealed class SubmitSurveyResponseCommandHandler
    : IRequestHandler<SubmitSurveyResponseCommand, Guid>
{
    private readonly IAppDbContext _db;

    public SubmitSurveyResponseCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(SubmitSurveyResponseCommand req, CancellationToken ct)
    {
        var survey = await _db.SurveyDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.SurveyId && s.IsActive && s.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Active SurveyDefinition {req.SurveyId} not found.");

        var response = SurveyResponse.Create(
            req.CorporationId, req.SurveyId, req.RespondentUserId,
            req.GuardianId, req.StudentId, req.SessionId);
        _db.SurveyResponses.Add(response);

        foreach (var ans in req.Answers)
        {
            _db.SurveyQuestionResponses.Add(SurveyQuestionResponse.Create(
                response.Id, ans.QuestionId, ans.AnswerText, ans.AnswerOptionId, ans.NumericValue));
        }

        response.Submit();
        await _db.SaveChangesAsync(ct);
        return response.Id;
    }
}

// ── CreateParentFeedbackCommand ───────────────────────────────────────────────

public record CreateParentFeedbackCommand(
    Guid CorporationId,
    Guid? GuardianId,
    Guid? EducatorId,
    Guid? SessionId,
    short? Rating,
    string? Comment) : IRequest<ParentFeedbackDto>;

public class CreateParentFeedbackCommandValidator
    : AbstractValidator<CreateParentFeedbackCommand>
{
    public CreateParentFeedbackCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Rating)
            .InclusiveBetween((short)1, (short)5)
            .When(x => x.Rating.HasValue)
            .WithMessage("Rating must be between 1 and 5.");
        RuleFor(x => x).Must(x => x.Rating.HasValue || !string.IsNullOrWhiteSpace(x.Comment))
            .WithMessage("At least a rating or a comment must be provided.");
    }
}

public sealed class CreateParentFeedbackCommandHandler
    : IRequestHandler<CreateParentFeedbackCommand, ParentFeedbackDto>
{
    private readonly IAppDbContext _db;

    public CreateParentFeedbackCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ParentFeedbackDto> Handle(CreateParentFeedbackCommand req, CancellationToken ct)
    {
        var feedback = ParentFeedback.Create(
            req.CorporationId, req.GuardianId, req.EducatorId,
            req.SessionId, req.Rating, req.Comment);
        _db.ParentFeedbacks.Add(feedback);

        await _db.SaveChangesAsync(ct);
        return new ParentFeedbackDto(
            feedback.Id, feedback.CorporationId,
            feedback.GuardianId, feedback.EducatorId, feedback.SessionId,
            feedback.Rating, feedback.Comment, feedback.CreatedAt);
    }
}
