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

public sealed class GetAssessmentHistoryQueryHandler
    : IRequestHandler<GetAssessmentHistoryQuery, IReadOnlyList<AssessmentSessionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetAssessmentHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AssessmentSessionListItemDto>> Handle(
        GetAssessmentHistoryQuery req, CancellationToken ct)
    {
        if (!req.LeadId.HasValue && !req.StudentId.HasValue)
            throw new ArgumentException("Either LeadId or StudentId must be provided.");

        var query = AssessmentProjection.BuildSessionListQuery(_db);

        if (req.LeadId.HasValue)
            query = query.Where(s => s.LeadId == req.LeadId);

        if (req.StudentId.HasValue)
            query = query.Where(s => s.StudentId == req.StudentId);

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }
}
