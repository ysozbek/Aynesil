using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Queries;

/// <summary>
/// Returns program recommendations filtered by session, lead, or student.
/// At least one filter must be provided.
/// </summary>
public record GetProgramRecommendationsQuery(
    Guid? AssessmentSessionId,
    Guid? LeadId,
    Guid? StudentId) : IRequest<IReadOnlyList<ProgramRecommendationDto>>;

public sealed class GetProgramRecommendationsQueryHandler
    : IRequestHandler<GetProgramRecommendationsQuery, IReadOnlyList<ProgramRecommendationDto>>
{
    private readonly IAppDbContext _db;

    public GetProgramRecommendationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProgramRecommendationDto>> Handle(
        GetProgramRecommendationsQuery req, CancellationToken ct)
    {
        var query = _db.ProgramRecommendations.AsNoTracking();

        if (req.AssessmentSessionId.HasValue)
            query = query.Where(r => r.AssessmentSessionId == req.AssessmentSessionId);

        if (req.LeadId.HasValue)
            query = query.Where(r => r.LeadId == req.LeadId);

        if (req.StudentId.HasValue)
            query = query.Where(r => r.StudentId == req.StudentId);

        var results = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return results.Select(AssessmentProjection.ToRecommendationDto).ToList().AsReadOnly();
    }
}
