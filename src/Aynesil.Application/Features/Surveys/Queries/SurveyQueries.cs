using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Surveys.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Surveys.Queries;

// ── GetSurveysQuery ───────────────────────────────────────────────────────────

public class GetSurveysQuery : PagedQuery, IRequest<PaginatedResult<SurveyDefinitionListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? TypeId { get; set; }
    public string? Target { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetSurveysQueryHandler
    : IRequestHandler<GetSurveysQuery, PaginatedResult<SurveyDefinitionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetSurveysQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<SurveyDefinitionListItemDto>> Handle(
        GetSurveysQuery req, CancellationToken ct)
    {
        var q = _db.SurveyDefinitions.AsNoTracking()
            .Where(s => s.DeletedAt == null);

        if (req.CorporationId.HasValue)
            q = q.Where(s => s.CorporationId == req.CorporationId.Value);

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
            select new SurveyDefinitionListItemDto(
                s.Id, s.TypeId, typ != null ? typ.Code : null,
                s.Title, s.Target, s.IsActive,
                s.Questions.Count(q2 => true),
                s.UpdatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "title"     => req.IsDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
            "updatedat" => req.IsDescending ? query.OrderByDescending(x => x.UpdatedAt) : query.OrderBy(x => x.UpdatedAt),
            _           => query.OrderByDescending(x => x.UpdatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<SurveyDefinitionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetSurveyQuery ────────────────────────────────────────────────────────────

public record GetSurveyQuery(Guid Id) : IRequest<SurveyDefinitionDto>;

public sealed class GetSurveyQueryHandler : IRequestHandler<GetSurveyQuery, SurveyDefinitionDto>
{
    private readonly IAppDbContext _db;

    public GetSurveyQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SurveyDefinitionDto> Handle(GetSurveyQuery req, CancellationToken ct)
    {
        var survey = await _db.SurveyDefinitions.AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(s => s.Id == req.Id && s.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"SurveyDefinition {req.Id} not found.");

        var typeCode = survey.TypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == survey.TypeId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        return new SurveyDefinitionDto(
            survey.Id, survey.CorporationId, survey.TypeId, typeCode,
            survey.Title, survey.Description, survey.Target, survey.IsActive,
            survey.CreatedAt, survey.UpdatedAt, survey.RowVersion,
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
            select new SurveyResponseListItemDto(
                r.Id, r.SurveyId, s.Title,
                r.GuardianId, r.StudentId,
                r.SubmittedAt, r.SubmittedAt != null);

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

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
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

    public GetParentFeedbackQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ParentFeedbackListItemDto>> Handle(
        GetParentFeedbackQuery req, CancellationToken ct)
    {
        var q = _db.ParentFeedbacks.AsNoTracking();

        if (req.GuardianId.HasValue)
            q = q.Where(f => f.GuardianId == req.GuardianId.Value);

        if (req.EducatorId.HasValue)
            q = q.Where(f => f.EducatorId == req.EducatorId.Value);

        if (req.SessionId.HasValue)
            q = q.Where(f => f.SessionId == req.SessionId.Value);

        var query = q.Select(f => new ParentFeedbackListItemDto(
            f.Id, f.GuardianId, f.EducatorId, f.SessionId,
            f.Rating, f.Comment, f.CreatedAt));

        query = req.IsDescending
            ? query.OrderByDescending(x => x.CreatedAt)
            : query.OrderBy(x => x.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<ParentFeedbackListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}
