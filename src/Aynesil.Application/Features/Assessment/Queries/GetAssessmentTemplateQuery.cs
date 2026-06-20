using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using MediatR;

namespace Aynesil.Application.Features.Assessment.Queries;

public record GetAssessmentTemplateQuery(Guid Id) : IRequest<AssessmentTemplateDto>;

public sealed class GetAssessmentTemplateQueryHandler
    : IRequestHandler<GetAssessmentTemplateQuery, AssessmentTemplateDto>
{
    private readonly IAppDbContext _db;

    public GetAssessmentTemplateQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AssessmentTemplateDto> Handle(
        GetAssessmentTemplateQuery req, CancellationToken ct)
        => await AssessmentProjection.LoadTemplateAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Assessment template {req.Id} not found.");
}
