using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── CreateCampReportCommand ───────────────────────────────────────────────────

public record CreateCampReportCommand(
    Guid CampEnrollmentId,
    string? Summary,
    Guid? FileId = null) : IRequest<CampReportDto>;

public class CreateCampReportCommandValidator : AbstractValidator<CreateCampReportCommand>
{
    public CreateCampReportCommandValidator()
    {
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
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public CreateCampReportCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<CampReportDto> Handle(CreateCampReportCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var enrollmentExists = await _db.CampEnrollments
            .AnyAsync(e => e.Id == req.CampEnrollmentId
                        && e.CorporationId == corporationId, ct);

        if (!enrollmentExists)
            throw new KeyNotFoundException($"Enrollment {req.CampEnrollmentId} not found.");

        var report = CampReport.Create(
            corporationId, req.CampEnrollmentId,
            req.Summary, req.FileId, _currentUser.UserId);

        _db.CampReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return new CampReportDto(
            report.Id, report.CampEnrollmentId,
            report.Summary, report.FileId,
            report.AuthoredBy, report.CreatedAt);
    }
}
