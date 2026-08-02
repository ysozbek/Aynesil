using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── CreateCampReportCommand ───────────────────────────────────────────────────

public record CreateCampReportCommand(
    Guid CorporationId,
    Guid CampEnrollmentId,
    string? Summary,
    Guid? FileId = null,
    Guid? AuthoredBy = null) : IRequest<CampReportDto>;

public class CreateCampReportCommandValidator : AbstractValidator<CreateCampReportCommand>
{
    public CreateCampReportCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CampEnrollmentId).NotEmpty();
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Summary) || x.FileId.HasValue)
            .WithMessage("A camp report must include at least a summary or an attached file.");
    }
}

public sealed class CreateCampReportCommandHandler
    : IRequestHandler<CreateCampReportCommand, CampReportDto>
{
    private readonly IAppDbContext _db;

    public CreateCampReportCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CampReportDto> Handle(CreateCampReportCommand req, CancellationToken ct)
    {
        var enrollmentExists = await _db.CampEnrollments
            .AnyAsync(e => e.Id == req.CampEnrollmentId
                        && e.CorporationId == req.CorporationId, ct);

        if (!enrollmentExists)
            throw new KeyNotFoundException($"Enrollment {req.CampEnrollmentId} not found.");

        var report = CampReport.Create(
            req.CorporationId, req.CampEnrollmentId,
            req.Summary, req.FileId, req.AuthoredBy);

        _db.CampReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return new CampReportDto(
            report.Id, report.CampEnrollmentId,
            report.Summary, report.FileId,
            report.AuthoredBy, report.CreatedAt);
    }
}
