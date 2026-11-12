using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using FluentValidation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Queries;

/// <summary>
/// Returns all assessment sessions for a lead or student — the reassessment history.
/// Used on the lead/student profile page to display the evaluation timeline.
/// Exactly one of LeadId or StudentId must be provided.
/// </summary>
public record GetAssessmentHistoryQuery(
    Guid? LeadId,
    Guid? StudentId) : IRequest<IReadOnlyList<AssessmentSessionListItemDto>>;

public sealed class GetAssessmentHistoryQueryValidator : AbstractValidator<GetAssessmentHistoryQuery>
{
    public GetAssessmentHistoryQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => x.LeadId.HasValue ^ x.StudentId.HasValue)
            .WithName(_ => Msg.Field("LeadId"))
            .WithKey(MsgKeys.Validation.ExactlyOneOf, Msg.FieldList("LeadId", "StudentId"));
        RuleFor(x => x.LeadId).NotEqual(Guid.Empty).When(x => x.LeadId.HasValue);
        RuleFor(x => x.StudentId).NotEqual(Guid.Empty).When(x => x.StudentId.HasValue);
    }
}

public sealed class GetAssessmentHistoryQueryHandler
    : IRequestHandler<GetAssessmentHistoryQuery, IReadOnlyList<AssessmentSessionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetAssessmentHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AssessmentSessionListItemDto>> Handle(
        GetAssessmentHistoryQuery req, CancellationToken ct)
    {
        var q = _db.AssessmentSessions.AsNoTracking();

        if (req.LeadId.HasValue)
            q = q.Where(s => s.LeadId == req.LeadId);

        if (req.StudentId.HasValue)
            q = q.Where(s => s.StudentId == req.StudentId);

        // Sıralama projeksiyondan ÖNCE entity üzerinden yapılır (ProjectSessionList notuna bkz.).
        return await AssessmentProjection.ProjectSessionList(_db, q.OrderByDescending(s => s.CreatedAt))
            .ToListAsync(ct);
    }
}
