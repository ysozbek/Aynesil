using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Queries;

public record GetAssessmentReportQuery(Guid AssessmentSessionId) : IRequest<AssessmentReportDto?>;

public sealed class GetAssessmentReportQueryHandler
    : IRequestHandler<GetAssessmentReportQuery, AssessmentReportDto?>
{
    private readonly IAppDbContext _db;

    public GetAssessmentReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AssessmentReportDto?> Handle(
        GetAssessmentReportQuery req, CancellationToken ct)
    {
        var report = await _db.AssessmentReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == req.AssessmentSessionId, ct);

        return report is null ? null : AssessmentProjection.ToReportDto(report);
    }
}
