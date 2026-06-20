using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── AddExternalInstitutionReportCommand ───────────────────────────────────────

public record AddExternalInstitutionReportCommand(
    Guid StudentId,
    string InstitutionName,
    Guid? InstitutionTypeId,
    DateOnly? ReportDate,
    string? Summary,
    Guid? FileId) : IRequest<ExternalInstitutionReportDto>;

public class AddExternalInstitutionReportCommandValidator
    : AbstractValidator<AddExternalInstitutionReportCommand>
{
    public AddExternalInstitutionReportCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.InstitutionName).NotEmpty().MaximumLength(500);
    }
}

public sealed class AddExternalInstitutionReportCommandHandler
    : IRequestHandler<AddExternalInstitutionReportCommand, ExternalInstitutionReportDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddExternalInstitutionReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ExternalInstitutionReportDto> Handle(
        AddExternalInstitutionReportCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var report = new ExternalInstitutionReport
        {
            CorporationId     = student.CorporationId,
            StudentId         = req.StudentId,
            InstitutionName   = req.InstitutionName,
            InstitutionTypeId = req.InstitutionTypeId,
            ReportDate        = req.ReportDate,
            Summary           = req.Summary,
            FileId            = req.FileId,
            CreatedAt         = DateTimeOffset.UtcNow,
            CreatedBy         = _currentUser.UserId
        };

        _db.ExternalInstitutionReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToExternalInstitutionReportDto(report);
    }
}

// ── DeleteExternalInstitutionReportCommand ────────────────────────────────────

public record DeleteExternalInstitutionReportCommand(Guid Id) : IRequest;

public sealed class DeleteExternalInstitutionReportCommandHandler
    : IRequestHandler<DeleteExternalInstitutionReportCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteExternalInstitutionReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteExternalInstitutionReportCommand req, CancellationToken ct)
    {
        var report = await _db.ExternalInstitutionReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"External institution report {req.Id} not found.");

        report.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
