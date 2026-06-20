using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using MediatR;

namespace Aynesil.Application.Features.Assessment.Queries;

public record GetAssessmentSessionQuery(Guid Id) : IRequest<AssessmentSessionDto>;

public sealed class GetAssessmentSessionQueryHandler
    : IRequestHandler<GetAssessmentSessionQuery, AssessmentSessionDto>
{
    private readonly IAppDbContext _db;

    public GetAssessmentSessionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AssessmentSessionDto> Handle(
        GetAssessmentSessionQuery req, CancellationToken ct)
        => await AssessmentProjection.LoadSessionAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Assessment session {req.Id} not found.");
}
