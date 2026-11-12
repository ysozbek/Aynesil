using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
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
            .WithKey(MsgKeys.Validation.AtLeastOneOf, Msg.FieldList("Summary", "AttachedFile"));
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
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Enrollment"));

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
