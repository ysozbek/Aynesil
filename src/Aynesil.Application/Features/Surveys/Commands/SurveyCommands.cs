using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Portal;
using Aynesil.Application.Features.Surveys.Dtos;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Surveys.Commands;

// ── CreateSurveyDefinitionCommand ─────────────────────────────────────────────

public record CreateSurveyDefinitionCommand(
    string Title,
    string Target,
    Guid? TypeId,
    string? Description,
    IReadOnlyList<QuestionInput> Questions) : IRequest<Guid>;

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
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Target).Must(t => ValidTargets.Contains(t))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Target"), EnumSets.SurveyTarget);
        RuleFor(x => x.Questions).NotEmpty()
            .WithKey(MsgKeys.Validation.AtLeastOneItem, Msg.FieldArg("Question"));
        RuleForEach(x => x.Questions).ChildRules(q =>
        {
            q.RuleFor(x => x.QuestionText).NotEmpty();
            q.RuleFor(x => x.QuestionType)
                .Must(t => ValidQuestionTypes.Contains(t))
                .WithKey(MsgKeys.Validation.InvalidQuestionType);
            q.RuleFor(x => x.AnswerOptions)
                .Must((x, o) => x.QuestionType != "multiple_choice" || o.Count >= 2)
                .WithKey(MsgKeys.Validation.ChoiceNeedsOptions);
            q.RuleForEach(x => x.AnswerOptions).ChildRules(o => o.RuleFor(x => x.OptionText).NotEmpty());
        });
    }
}

public sealed class CreateSurveyDefinitionCommandHandler
    : IRequestHandler<CreateSurveyDefinitionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public CreateSurveyDefinitionCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<Guid> Handle(CreateSurveyDefinitionCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var survey = SurveyDefinition.Create(
            corporationId, req.Title, req.Target,
            req.TypeId, req.Description, _currentUser.UserId);
        _db.SurveyDefinitions.Add(survey);

        foreach (var q in req.Questions.OrderBy(x => x.SortOrder))
        {
            var question = SurveyQuestion.Create(
                survey.Id, q.QuestionText, q.QuestionType, q.IsRequired, q.SortOrder, _currentUser.UserId);
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

/// <summary>
/// Header fields always update. <c>Questions</c> (null = leave questions untouched) replaces the question
/// list: items with an Id are updated in place, new ones are added, missing ones are removed. Once a survey
/// has responses its questions are locked (answers point at them) → 422 <c>Rule.SurveyQuestionsLocked</c>.
/// </summary>
public record UpdateSurveyDefinitionCommand(
    Guid Id,
    string Title,
    string? Description,
    string Target,
    Guid? TypeId,
    bool IsActive,
    int RowVersion,
    IReadOnlyList<QuestionUpdateInput>? Questions = null) : IRequest;

public record QuestionUpdateInput(
    Guid? Id,
    string QuestionText,
    string QuestionType,
    bool IsRequired,
    int SortOrder,
    IReadOnlyList<AnswerOptionUpdateInput> AnswerOptions);

public record AnswerOptionUpdateInput(Guid? Id, string OptionText, string? OptionValue, int SortOrder);

public class UpdateSurveyDefinitionCommandValidator
    : AbstractValidator<UpdateSurveyDefinitionCommand>
{
    private static readonly string[] ValidTargets = ["guardian", "educator", "student"];
    private static readonly string[] ValidQuestionTypes =
        ["text", "rating", "yes_no", "multiple_choice", "scale"];

    public UpdateSurveyDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Target).Must(t => ValidTargets.Contains(t))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Target"), EnumSets.SurveyTarget);
        When(x => x.Questions != null, () =>
        {
            RuleFor(x => x.Questions!).NotEmpty()
                .WithKey(MsgKeys.Validation.AtLeastOneItem, Msg.FieldArg("Question"));
            RuleForEach(x => x.Questions!).ChildRules(q =>
            {
                q.RuleFor(x => x.QuestionText).NotEmpty();
                q.RuleFor(x => x.QuestionType)
                    .Must(t => ValidQuestionTypes.Contains(t))
                    .WithKey(MsgKeys.Validation.InvalidQuestionType);
                q.RuleFor(x => x.AnswerOptions)
                    .Must((x, o) => x.QuestionType != "multiple_choice" || o.Count >= 2)
                    .WithKey(MsgKeys.Validation.ChoiceNeedsOptions);
                q.RuleForEach(x => x.AnswerOptions).ChildRules(o => o.RuleFor(x => x.OptionText).NotEmpty());
            });
        });
    }
}

public sealed class UpdateSurveyDefinitionCommandHandler
    : IRequestHandler<UpdateSurveyDefinitionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateSurveyDefinitionCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task Handle(UpdateSurveyDefinitionCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();
        var survey = await _db.SurveyDefinitions
            .Include(s => s.Questions).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(s => s.Id == req.Id && s.CorporationId == corporationId && s.DeletedAt == null, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "SurveyDefinition"));

        if (req.Questions is not null)
        {
            if (await _db.SurveyResponses.AnyAsync(r => r.SurveyId == req.Id, ct))
                throw new BusinessRuleViolationException(MsgKeys.Rule.SurveyQuestionsLocked);
            SyncQuestions(survey, req.Questions);
        }

        survey.Update(req.Title, req.Description, req.Target, req.TypeId, req.IsActive);
        await _db.SaveChangesAsync(ct);
    }

    private void SyncQuestions(SurveyDefinition survey, IReadOnlyList<QuestionUpdateInput> inputs)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        var existing = survey.Questions.ToDictionary(q => q.Id);
        for (var i = 0; i < inputs.Count; i++)
        {
            if (inputs[i].Id is { } qid && !existing.ContainsKey(qid))
                failures.Add(ValidationFailures.For($"Questions[{i}].Id", MsgKeys.Validation.NotPartOfSurvey));
            else if (inputs[i].Id is { } oqid)
            {
                var known = existing[oqid].AnswerOptions.Select(o => o.Id).ToHashSet();
                for (var j = 0; j < inputs[i].AnswerOptions.Count; j++)
                    if (inputs[i].AnswerOptions[j].Id is { } oid && !known.Contains(oid))
                        failures.Add(ValidationFailures.For($"Questions[{i}].AnswerOptions[{j}].Id", MsgKeys.Validation.NotPartOfSurvey));
            }
        }
        if (failures.Count > 0)
            throw new Aynesil.Application.Common.Exceptions.ValidationException(failures);

        var keptIds = inputs.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        foreach (var gone in survey.Questions.Where(q => !keptIds.Contains(q.Id)).ToList())
        {
            _db.SurveyAnswerOptions.RemoveRange(gone.AnswerOptions);
            _db.SurveyQuestions.Remove(gone);
        }

        foreach (var input in inputs)
        {
            SurveyQuestion question;
            if (input.Id is { } id)
            {
                question = existing[id];
                question.Update(input.QuestionText, input.QuestionType, input.IsRequired, input.SortOrder);
            }
            else
            {
                question = SurveyQuestion.Create(
                    survey.Id, input.QuestionText, input.QuestionType, input.IsRequired, input.SortOrder);
                _db.SurveyQuestions.Add(question);
            }

            var optionIds = input.AnswerOptions.Where(o => o.Id.HasValue).Select(o => o.Id!.Value).ToHashSet();
            var currentOptions = question.AnswerOptions.ToDictionary(o => o.Id);
            foreach (var goneOpt in currentOptions.Values.Where(o => !optionIds.Contains(o.Id)).ToList())
                _db.SurveyAnswerOptions.Remove(goneOpt);
            foreach (var opt in input.AnswerOptions)
            {
                if (opt.Id is { } oid)
                    currentOptions[oid].Update(opt.OptionText, opt.OptionValue, opt.SortOrder);
                else
                    _db.SurveyAnswerOptions.Add(
                        SurveyAnswerOption.Create(question.Id, opt.OptionText, opt.OptionValue, opt.SortOrder));
            }
        }
    }
}

// ── DeleteSurveyDefinitionCommand ─────────────────────────────────────────────

public record DeleteSurveyDefinitionCommand(Guid Id) : IRequest;

public sealed class DeleteSurveyDefinitionCommandHandler
    : IRequestHandler<DeleteSurveyDefinitionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public DeleteSurveyDefinitionCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task Handle(DeleteSurveyDefinitionCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();
        var survey = await _db.SurveyDefinitions
            .FirstOrDefaultAsync(s => s.Id == req.Id && s.CorporationId == corporationId && s.DeletedAt == null, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "SurveyDefinition"));

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

/// <summary>
/// RespondentUserId is the signed-in user. When the caller is a portal guardian, GuardianId is
/// derived from the session and StudentId must be one of that guardian's students; staff may
/// submit on a guardian's behalf with a GuardianId from the current tenant.
/// </summary>
public record SubmitSurveyResponseCommand(
    Guid SurveyId,
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
        RuleFor(x => x.SurveyId).NotEmpty();
        RuleFor(x => x.Answers).NotEmpty()
            .WithKey(MsgKeys.Validation.AtLeastOneItem, Msg.FieldArg("Answer"));
        RuleFor(x => x.Answers)
            .Must(a => a.Select(x => x.QuestionId).Distinct().Count() == a.Count)
            .WithKey(MsgKeys.Validation.DuplicateAnswer);
    }
}

public sealed class SubmitSurveyResponseCommandHandler
    : IRequestHandler<SubmitSurveyResponseCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public SubmitSurveyResponseCommandHandler(
        IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(SubmitSurveyResponseCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var survey = await _db.SurveyDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.CorporationId == corporationId && s.Id == req.SurveyId && s.IsActive && s.DeletedAt == null, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "ActiveSurveyDefinition"));

        var userId = _currentUser.UserId;
        var (guardianId, isGuardian) = await PortalGuardian.ResolveAsync(
            _db, corporationId, userId, req.GuardianId, ct);
        if (guardianId.HasValue && survey.Target != "guardian")
            throw new BusinessRuleViolationException(MsgKeys.Rule.SurveyTargetMismatch);
        if (isGuardian && req.StudentId.HasValue)
            await PortalGuardian.EnsureCanAccessStudentAsync(
                _db, corporationId, guardianId!.Value, req.StudentId.Value, ct);
        if (isGuardian && req.SessionId.HasValue)
            await EnsureGuardianSessionAsync(corporationId, guardianId!.Value, req.SessionId.Value, ct);

        await EnsureAnswersBelongToSurveyAsync(req, ct);
        await EnsureNotAnsweredBeforeAsync(corporationId, req, guardianId, userId, ct);

        var response = SurveyResponse.Create(
            corporationId, req.SurveyId, userId,
            guardianId, req.StudentId, req.SessionId);
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

    /// <summary>
    /// One submitted response per survey × respondent × student × session (table comment on
    /// ops.survey_response). A second submission for the same combination → 422.
    /// </summary>
    private async Task EnsureNotAnsweredBeforeAsync(
        Guid corporationId, SubmitSurveyResponseCommand req, Guid? guardianId, Guid? userId, CancellationToken ct)
    {
        var q = _db.SurveyResponses.AsNoTracking().Where(r =>
            r.CorporationId == corporationId && r.SurveyId == req.SurveyId && r.SubmittedAt != null
            && r.StudentId == req.StudentId && r.SessionId == req.SessionId);
        q = guardianId.HasValue
            ? q.Where(r => r.GuardianId == guardianId)
            : q.Where(r => r.GuardianId == null && r.RespondentUserId == userId);
        if (await q.AnyAsync(ct))
            throw new BusinessRuleViolationException(MsgKeys.Rule.SurveyAlreadySubmitted);
    }

    /// <summary>A guardian can link a response only to a session of one of their own (accessible) children.</summary>
    private async Task EnsureGuardianSessionAsync(
        Guid corporationId, Guid guardianId, Guid sessionId, CancellationToken ct)
    {
        var ok = await (
            from sp in _db.SessionParticipants.AsNoTracking()
            join gpa in _db.GuardianPortalAccesses.AsNoTracking() on sp.StudentId equals gpa.StudentId
            where sp.SessionId == sessionId && sp.CorporationId == corporationId
               && gpa.CorporationId == corporationId && gpa.GuardianId == guardianId && gpa.RevokedAt == null
            select sp.Id).AnyAsync(ct);
        if (!ok) throw new ForbiddenAccessException(MsgKeys.Rule.GuardianNoActiveAccess);
    }

    /// <summary>
    /// Y6: each answer's question must belong to the submitted survey, and a chosen option to that
    /// question. survey_question / survey_answer_option have no corporation_id, so without this check an
    /// answer could point at another survey's (or another tenant's) question. Mismatch → 422.
    /// Also: every required question needs an answer, and rating answers are 1–5.
    /// </summary>
    private async Task EnsureAnswersBelongToSurveyAsync(SubmitSurveyResponseCommand req, CancellationToken ct)
    {
        var questions = await _db.SurveyQuestions.AsNoTracking()
            .Where(q => q.SurveyId == req.SurveyId)
            .Select(q => new { q.Id, q.QuestionType, q.IsRequired })
            .ToListAsync(ct);
        var questionIds = questions.Select(q => q.Id).ToList();
        var optionIds = req.Answers.Any(a => a.AnswerOptionId.HasValue)
            ? await _db.SurveyAnswerOptions.AsNoTracking()
                .Where(o => questionIds.Contains(o.QuestionId))
                .Select(o => new { o.Id, o.QuestionId })
                .ToDictionaryAsync(o => o.Id, o => o.QuestionId, ct)
            : new Dictionary<Guid, Guid>();

        var failures = new List<FluentValidation.Results.ValidationFailure>();
        for (var i = 0; i < req.Answers.Count; i++)
        {
            var answer = req.Answers[i];
            if (!questionIds.Contains(answer.QuestionId))
                failures.Add(ValidationFailures.For($"Answers[{i}].QuestionId", MsgKeys.Validation.NotPartOfSurvey));
            else if (answer.AnswerOptionId is { } optionId
                     && (!optionIds.TryGetValue(optionId, out var optionQuestion) || optionQuestion != answer.QuestionId))
                failures.Add(ValidationFailures.For($"Answers[{i}].AnswerOptionId", MsgKeys.Validation.NotPartOfSurvey));
            else if (answer.NumericValue is { } n
                     && questions.First(q => q.Id == answer.QuestionId).QuestionType == "rating" && (n < 1 || n > 5))
                failures.Add(ValidationFailures.For($"Answers[{i}].NumericValue",
                    MsgKeys.Validation.MustBeBetween, Msg.FieldArg("Rating"), 1, 5));
        }

        foreach (var q in questions.Where(q => q.IsRequired))
        {
            var given = req.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            if (given is null || (string.IsNullOrWhiteSpace(given.AnswerText)
                                  && !given.AnswerOptionId.HasValue && !given.NumericValue.HasValue))
                failures.Add(ValidationFailures.For($"Answers[{q.Id}]", MsgKeys.Validation.RequiredQuestionUnanswered));
        }

        if (failures.Count > 0)
            throw new Aynesil.Application.Common.Exceptions.ValidationException(failures);
    }
}

// ── CreateParentFeedbackCommand ───────────────────────────────────────────────

public record CreateParentFeedbackCommand(
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
        RuleFor(x => x.Rating)
            .InclusiveBetween((short)1, (short)5)
            .When(x => x.Rating.HasValue)
            .WithKey(MsgKeys.Validation.MustBeBetween, Msg.FieldArg("Rating"), 1, 5);
        RuleFor(x => x).Must(x => x.Rating.HasValue || !string.IsNullOrWhiteSpace(x.Comment))
            .WithKey(MsgKeys.Validation.AtLeastOneOf, Msg.FieldList("Rating", "Comment"));
        RuleFor(x => x.Comment).MaximumLength(2000).When(x => x.Comment != null);
    }
}

public sealed class CreateParentFeedbackCommandHandler
    : IRequestHandler<CreateParentFeedbackCommand, ParentFeedbackDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public CreateParentFeedbackCommandHandler(
        IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ParentFeedbackDto> Handle(CreateParentFeedbackCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();
        var (guardianId, isGuardian) = await PortalGuardian.ResolveAsync(
            _db, corporationId, _currentUser.UserId, req.GuardianId, ct);

        if (req.EducatorId.HasValue
            && !await _db.Educators.AnyAsync(e => e.Id == req.EducatorId && e.CorporationId == corporationId && e.DeletedAt == null, ct))
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Educator"));
        if (req.SessionId.HasValue
            && !await _db.Sessions.AnyAsync(s => s.Id == req.SessionId && s.CorporationId == corporationId && s.DeletedAt == null, ct))
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Session"));

        // A guardian rates only people/sessions tied to their own (portal-accessible) children.
        if (isGuardian && (req.SessionId.HasValue || req.EducatorId.HasValue))
        {
            var childSessions =
                from sp in _db.SessionParticipants.AsNoTracking()
                join gpa in _db.GuardianPortalAccesses.AsNoTracking() on sp.StudentId equals gpa.StudentId
                where sp.CorporationId == corporationId && gpa.CorporationId == corporationId
                   && gpa.GuardianId == guardianId && gpa.RevokedAt == null
                select sp.SessionId;
            if (req.SessionId.HasValue && !await childSessions.AnyAsync(id => id == req.SessionId, ct))
                throw new ForbiddenAccessException(MsgKeys.Rule.GuardianNoActiveAccess);
            if (req.EducatorId.HasValue)
            {
                var taught = _db.SessionEducators.AsNoTracking()
                    .Where(se => se.CorporationId == corporationId && se.EducatorId == req.EducatorId
                              && childSessions.Contains(se.SessionId));
                if (req.SessionId.HasValue) taught = taught.Where(se => se.SessionId == req.SessionId);
                if (!await taught.AnyAsync(ct))
                    throw new ForbiddenAccessException(MsgKeys.Rule.GuardianNoActiveAccess);
            }
        }

        // Rating a session without naming the educator credits the session's lead educator (KPI parent
        // satisfaction is per educator); several educators and no lead → left unset.
        var educatorId = req.EducatorId;
        if (educatorId is null && req.SessionId.HasValue)
        {
            var assigned = await _db.SessionEducators.AsNoTracking()
                .Where(se => se.CorporationId == corporationId && se.SessionId == req.SessionId)
                .Select(se => new { se.EducatorId, se.Role })
                .ToListAsync(ct);
            educatorId = assigned.Count == 1
                ? assigned[0].EducatorId
                : assigned.Where(a => a.Role == "lead").Select(a => (Guid?)a.EducatorId).FirstOrDefault();
        }

        var feedback = ParentFeedback.Create(
            corporationId, guardianId, educatorId,
            req.SessionId, req.Rating, req.Comment);
        _db.ParentFeedbacks.Add(feedback);

        await _db.SaveChangesAsync(ct);
        return new ParentFeedbackDto(
            feedback.Id, feedback.CorporationId,
            feedback.GuardianId, feedback.EducatorId, feedback.SessionId,
            feedback.Rating, feedback.Comment, feedback.CreatedAt);
    }
}
