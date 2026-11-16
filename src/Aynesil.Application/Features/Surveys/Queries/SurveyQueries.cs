using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Portal;
using Aynesil.Application.Features.Surveys.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Surveys.Queries;

// ── GetSurveysQuery ───────────────────────────────────────────────────────────

public class GetSurveysQuery : PagedQuery, IRequest<PaginatedResult<SurveyDefinitionListItemDto>>
{
    public Guid? TypeId { get; set; }
    public string? Target { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetSurveysQueryHandler
    : IRequestHandler<GetSurveysQuery, PaginatedResult<SurveyDefinitionListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetSurveysQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResult<SurveyDefinitionListItemDto>> Handle(
        GetSurveysQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var q = _db.SurveyDefinitions.AsNoTracking()
            .Where(s => s.DeletedAt == null);

        q = q.Where(s => s.CorporationId == corporationId);

        if (req.TypeId.HasValue)
            q = q.Where(s => s.TypeId == req.TypeId.Value);

        if (!string.IsNullOrWhiteSpace(req.Target))
            q = q.Where(s => s.Target == req.Target);

        if (req.IsActive.HasValue)
            q = q.Where(s => s.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(x => x.Title.ToLower().Contains(s));
        }

        var query =
            from s in q
            join typ in _db.RefValues.AsNoTracking()
                on s.TypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new
            {
                s.Id,
                s.TypeId,
                TypeCode = typ != null ? typ.Code : null,
                s.Title,
                s.Target,
                s.IsActive,
                QuestionCount = s.Questions.Count(q2 => true),
                ResponseCount = _db.SurveyResponses.Count(r => r.SurveyId == s.Id && r.SubmittedAt != null),
                s.UpdatedAt
            };

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "title"     => req.IsDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
            "updatedat" => req.IsDescending ? query.OrderByDescending(x => x.UpdatedAt) : query.OrderBy(x => x.UpdatedAt),
            _           => query.OrderByDescending(x => x.UpdatedAt)
        };

        var projected = query.Select(r =>
                new SurveyDefinitionListItemDto(
                    r.Id,
                    r.TypeId,
                    r.TypeCode,
                    r.Title,
                    r.Target,
                    r.IsActive,
                    r.QuestionCount,
                    r.ResponseCount,
                    r.UpdatedAt));

        var total = await projected.CountAsync(ct);
        var items = await projected.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<SurveyDefinitionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetSurveyQuery ────────────────────────────────────────────────────────────

public record GetSurveyQuery(Guid Id) : IRequest<SurveyDefinitionDto>;

public sealed class GetSurveyQueryHandler : IRequestHandler<GetSurveyQuery, SurveyDefinitionDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetSurveyQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<SurveyDefinitionDto> Handle(GetSurveyQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();
        var survey = await _db.SurveyDefinitions.AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(s => s.Id == req.Id && s.CorporationId == corporationId && s.DeletedAt == null, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "SurveyDefinition"));

        var typeCode = survey.TypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == survey.TypeId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var responseCount = await _db.SurveyResponses.CountAsync(
            r => r.SurveyId == survey.Id && r.SubmittedAt != null, ct);

        return new SurveyDefinitionDto(
            survey.Id, survey.CorporationId, survey.TypeId, typeCode,
            survey.Title, survey.Description, survey.Target, survey.IsActive,
            survey.CreatedAt, survey.UpdatedAt, survey.RowVersion, responseCount,
            survey.Questions
                .OrderBy(q => q.SortOrder)
                .Select(q => new SurveyQuestionDto(
                    q.Id, q.QuestionText, q.QuestionType, q.IsRequired, q.SortOrder,
                    q.AnswerOptions
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new SurveyAnswerOptionDto(
                            o.Id, o.OptionText, o.OptionValue, o.SortOrder))
                        .ToList()))
                .ToList());
    }
}

// ── GetSurveyResponsesQuery ───────────────────────────────────────────────────

public class GetSurveyResponsesQuery
    : PagedQuery, IRequest<PaginatedResult<SurveyResponseListItemDto>>
{
    public Guid? SurveyId { get; set; }
    public Guid? GuardianId { get; set; }
    public Guid? StudentId { get; set; }
    public bool? IsSubmitted { get; set; }
}

public sealed class GetSurveyResponsesQueryHandler
    : IRequestHandler<GetSurveyResponsesQuery, PaginatedResult<SurveyResponseListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetSurveyResponsesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<SurveyResponseListItemDto>> Handle(
        GetSurveyResponsesQuery req, CancellationToken ct)
    {
        var query =
            from r in _db.SurveyResponses.AsNoTracking()
            join s in _db.SurveyDefinitions.AsNoTracking()
                on r.SurveyId equals s.Id
            join g in _db.Guardians.AsNoTracking() on r.GuardianId equals g.Id into gg
            from g in gg.DefaultIfEmpty()
            join st in _db.Students.AsNoTracking() on r.StudentId equals st.Id into sg
            from st in sg.DefaultIfEmpty()
            select new
            {
                r.Id,
                r.SurveyId,
                SurveyTitle = s.Title,
                r.GuardianId,
                GuardianName = g != null ? g.FirstName + " " + g.LastName : null,
                r.StudentId,
                StudentName = st != null ? st.FirstName + " " + st.LastName : null,
                r.SubmittedAt,
                IsComplete = r.SubmittedAt != null
            };

        if (req.SurveyId.HasValue)
            query = query.Where(x => x.SurveyId == req.SurveyId.Value);

        if (req.GuardianId.HasValue)
            query = query.Where(x => x.GuardianId == req.GuardianId.Value);

        if (req.StudentId.HasValue)
            query = query.Where(x => x.StudentId == req.StudentId.Value);

        if (req.IsSubmitted.HasValue)
        {
            if (req.IsSubmitted.Value)
                query = query.Where(x => x.SubmittedAt != null);
            else
                query = query.Where(x => x.SubmittedAt == null);
        }

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "submittedat" => req.IsDescending
                ? query.OrderByDescending(x => x.SubmittedAt)
                : query.OrderBy(x => x.SubmittedAt),
            _ => query.OrderByDescending(x => x.SubmittedAt)
        };

        var projected = query.Select(r =>
                new SurveyResponseListItemDto(
                    r.Id,
                    r.SurveyId,
                    r.SurveyTitle,
                    r.GuardianId,
                    r.GuardianName,
                    r.StudentId,
                    r.StudentName,
                    r.SubmittedAt,
                    r.IsComplete));

        var total = await projected.CountAsync(ct);
        var items = await projected.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<SurveyResponseListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetParentFeedbackQuery ────────────────────────────────────────────────────

public class GetParentFeedbackQuery
    : PagedQuery, IRequest<PaginatedResult<ParentFeedbackListItemDto>>
{
    public Guid? GuardianId { get; set; }
    public Guid? EducatorId { get; set; }
    public Guid? SessionId { get; set; }
}

public sealed class GetParentFeedbackQueryHandler
    : IRequestHandler<GetParentFeedbackQuery, PaginatedResult<ParentFeedbackListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public GetParentFeedbackQueryHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<ParentFeedbackListItemDto>> Handle(
        GetParentFeedbackQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();
        // A guardian never lists other guardians' feedback, whatever filter they send.
        var own = await PortalGuardian.FindCurrentGuardianIdAsync(_db, corporationId, _currentUser.UserId, ct);
        return await FeedbackPage.QueryAsync(_db, corporationId, req, own ?? req.GuardianId, own.HasValue, ct);
    }
}

/// <summary>Own feedback of the signed-in guardian (parent portal list). Staff get an empty page.</summary>
public class GetMyParentFeedbackQuery : PagedQuery, IRequest<PaginatedResult<ParentFeedbackListItemDto>> { }

public sealed class GetMyParentFeedbackQueryHandler
    : IRequestHandler<GetMyParentFeedbackQuery, PaginatedResult<ParentFeedbackListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public GetMyParentFeedbackQueryHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<ParentFeedbackListItemDto>> Handle(
        GetMyParentFeedbackQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();
        var own = await PortalGuardian.FindCurrentGuardianIdAsync(_db, corporationId, _currentUser.UserId, ct);
        if (!own.HasValue)
            return PaginatedResult<ParentFeedbackListItemDto>.Create([], 0, req.Page, req.PageSize);
        return await FeedbackPage.QueryAsync(
            _db, corporationId, new GetParentFeedbackQuery { Page = req.Page, PageSize = req.PageSize, SortDirection = "desc" },
            own, true, ct);
    }
}

internal static class FeedbackPage
{
    public static async Task<PaginatedResult<ParentFeedbackListItemDto>> QueryAsync(
        IAppDbContext db, Guid corporationId, GetParentFeedbackQuery req, Guid? guardianId, bool forceGuardian,
        CancellationToken ct)
    {
        var q = db.ParentFeedbacks.AsNoTracking().Where(f => f.CorporationId == corporationId);

        if (guardianId.HasValue)
            q = q.Where(f => f.GuardianId == guardianId.Value);
        else if (forceGuardian)
            q = q.Where(f => false);

        if (req.EducatorId.HasValue)
            q = q.Where(f => f.EducatorId == req.EducatorId.Value);

        if (req.SessionId.HasValue)
            q = q.Where(f => f.SessionId == req.SessionId.Value);

        var query =
            from f in q
            join g in db.Guardians.AsNoTracking() on f.GuardianId equals g.Id into gg
            from g in gg.DefaultIfEmpty()
            join e in db.Educators.AsNoTracking() on f.EducatorId equals e.Id into eg
            from e in eg.DefaultIfEmpty()
            select new
            {
                f.Id,
                f.GuardianId,
                GuardianName = g != null ? g.FirstName + " " + g.LastName : null,
                f.EducatorId,
                EducatorName = e != null ? e.FirstName + " " + e.LastName : null,
                f.SessionId,
                f.Rating,
                f.Comment,
                f.CreatedAt
            };

        query = req.IsDescending
            ? query.OrderByDescending(x => x.CreatedAt)
            : query.OrderBy(x => x.CreatedAt);

        var projected = query.Select(r =>
            new ParentFeedbackListItemDto(
                r.Id, r.GuardianId, r.GuardianName, r.EducatorId, r.EducatorName,
                r.SessionId, r.Rating, r.Comment, r.CreatedAt));

        var total = await projected.CountAsync(ct);
        var items = await projected.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<ParentFeedbackListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetSurveyResultsQuery ─────────────────────────────────────────────────────

/// <summary>Per-question summary of the submitted responses of one survey.</summary>
public record GetSurveyResultsQuery(Guid Id) : IRequest<SurveyResultsDto>;

public sealed class GetSurveyResultsQueryHandler : IRequestHandler<GetSurveyResultsQuery, SurveyResultsDto>
{
    private const int MaxTextAnswers = 50;

    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetSurveyResultsQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<SurveyResultsDto> Handle(GetSurveyResultsQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();
        var survey = await _db.SurveyDefinitions.AsNoTracking()
            .Include(s => s.Questions).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(s => s.Id == req.Id && s.CorporationId == corporationId && s.DeletedAt == null, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "SurveyDefinition"));

        var submitted = _db.SurveyResponses.AsNoTracking()
            .Where(r => r.CorporationId == corporationId && r.SurveyId == survey.Id && r.SubmittedAt != null);
        var responseCount = await submitted.CountAsync(ct);
        var last = responseCount == 0 ? null : await submitted.MaxAsync(r => r.SubmittedAt, ct);

        var answers = await (
            from a in _db.SurveyQuestionResponses.AsNoTracking()
            join r in submitted on a.ResponseId equals r.Id
            orderby r.SubmittedAt descending
            select new { a.QuestionId, a.AnswerText, a.AnswerOptionId, a.NumericValue }).ToListAsync(ct);
        var byQuestion = answers.ToLookup(a => a.QuestionId);

        var questions = survey.Questions.OrderBy(q => q.SortOrder).Select(q =>
        {
            var given = byQuestion[q.Id].ToList();
            var options = q.AnswerOptions.OrderBy(o => o.SortOrder).ToList();
            var distribution = new List<SurveyResultBucketDto>();
            var texts = new List<string>();
            decimal? average = null;

            switch (q.QuestionType)
            {
                case "rating":
                case "scale" when options.Count == 0:
                    var values = given.Where(a => a.NumericValue.HasValue).Select(a => a.NumericValue!.Value).ToList();
                    if (values.Count > 0) average = Math.Round(values.Average(), 2);
                    var lo = q.QuestionType == "rating" ? 1 : (int)Math.Floor(values.DefaultIfEmpty(1).Min());
                    var hi = q.QuestionType == "rating" ? 5 : (int)Math.Ceiling(values.DefaultIfEmpty(1).Max());
                    for (var v = lo; v <= hi; v++)
                        distribution.Add(new SurveyResultBucketDto(
                            v.ToString(), v.ToString(), values.Count(x => Math.Round(x) == v)));
                    break;

                case "multiple_choice":
                case "scale":
                    foreach (var o in options)
                        distribution.Add(new SurveyResultBucketDto(
                            o.OptionText, o.OptionValue, given.Count(a => a.AnswerOptionId == o.Id)));
                    var scored = given.Join(options, a => a.AnswerOptionId, o => o.Id, (a, o) => o.OptionValue)
                        .Select(v => decimal.TryParse(v, System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.InvariantCulture, out var d) ? (decimal?)d : null)
                        .Where(d => d.HasValue).Select(d => d!.Value).ToList();
                    if (scored.Count > 0 && scored.Count == given.Count(a => a.AnswerOptionId.HasValue))
                        average = Math.Round(scored.Average(), 2);
                    break;

                case "yes_no":
                    foreach (var code in new[] { "yes", "no" })
                        distribution.Add(new SurveyResultBucketDto(code, code,
                            given.Count(a => string.Equals(a.AnswerText?.Trim(), code, StringComparison.OrdinalIgnoreCase))));
                    break;

                default:
                    texts = given.Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                        .Select(a => a.AnswerText!.Trim()).Take(MaxTextAnswers).ToList();
                    break;
            }

            var answered = q.QuestionType == "text"
                ? given.Count(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                : given.Count(a => a.NumericValue.HasValue || a.AnswerOptionId.HasValue
                                   || !string.IsNullOrWhiteSpace(a.AnswerText));
            return new SurveyQuestionResultDto(
                q.Id, q.QuestionText, q.QuestionType, q.IsRequired, answered, average, distribution, texts);
        }).ToList();

        return new SurveyResultsDto(survey.Id, survey.Title, survey.Target, responseCount, last, questions);
    }
}

// ── GetPendingSurveysQuery (parent portal) ────────────────────────────────────

/// <summary>
/// Active guardian-target surveys the signed-in guardian has not yet answered, per portal-accessible child.
/// A staff user (not a guardian) gets an empty list.
/// </summary>
public record GetPendingSurveysQuery : IRequest<IReadOnlyList<PendingSurveyDto>>;

public sealed class GetPendingSurveysQueryHandler
    : IRequestHandler<GetPendingSurveysQuery, IReadOnlyList<PendingSurveyDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public GetPendingSurveysQueryHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PendingSurveyDto>> Handle(GetPendingSurveysQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();
        var guardianId = await PortalGuardian.FindCurrentGuardianIdAsync(_db, corporationId, _currentUser.UserId, ct);
        if (!guardianId.HasValue) return [];

        var children = await (
            from gpa in _db.GuardianPortalAccesses.AsNoTracking()
            join st in _db.Students.AsNoTracking() on gpa.StudentId equals st.Id
            where gpa.CorporationId == corporationId && gpa.GuardianId == guardianId && gpa.RevokedAt == null
               && st.DeletedAt == null
            orderby st.FirstName, st.LastName
            select new PendingSurveyStudentDto(st.Id, st.FirstName + " " + st.LastName)).ToListAsync(ct);
        if (children.Count == 0) return [];

        var surveys = await _db.SurveyDefinitions.AsNoTracking()
            .Include(s => s.Questions).ThenInclude(q => q.AnswerOptions)
            .Where(s => s.CorporationId == corporationId && s.DeletedAt == null && s.IsActive && s.Target == "guardian")
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
        if (surveys.Count == 0) return [];

        var surveyIds = surveys.Select(s => s.Id).ToList();
        var answered = (await _db.SurveyResponses.AsNoTracking()
            .Where(r => r.CorporationId == corporationId && r.GuardianId == guardianId
                     && r.SubmittedAt != null && r.SessionId == null && surveyIds.Contains(r.SurveyId))
            .Select(r => new { r.SurveyId, r.StudentId })
            .ToListAsync(ct)).ToHashSet();

        var typeIds = surveys.Where(s => s.TypeId.HasValue).Select(s => s.TypeId!.Value).Distinct().ToList();
        var typeCodes = typeIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.RefValues.AsNoTracking().Where(r => typeIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Code, ct);

        var result = new List<PendingSurveyDto>();
        foreach (var s in surveys.Where(s => s.Questions.Count > 0))
        {
            var open = children.Where(c => !answered.Contains(new { SurveyId = s.Id, StudentId = (Guid?)c.StudentId })).ToList();
            if (open.Count == 0) continue;
            result.Add(new PendingSurveyDto(
                s.Id, s.Title, s.Description,
                s.TypeId.HasValue && typeCodes.TryGetValue(s.TypeId.Value, out var code) ? code : null,
                s.Questions.OrderBy(q => q.SortOrder).Select(q => new SurveyQuestionDto(
                    q.Id, q.QuestionText, q.QuestionType, q.IsRequired, q.SortOrder,
                    q.AnswerOptions.OrderBy(o => o.SortOrder)
                        .Select(o => new SurveyAnswerOptionDto(o.Id, o.OptionText, o.OptionValue, o.SortOrder)).ToList()))
                    .ToList(),
                open));
        }
        return result;
    }
}
