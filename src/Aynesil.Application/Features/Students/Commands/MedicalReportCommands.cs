using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── AddMedicalReportCommand ───────────────────────────────────────────────────

public record AddMedicalReportCommand(
    Guid StudentId,
    string Title,
    DateOnly? ReportDate,
    string? Issuer,
    string? Summary,
    Guid? FileId) : IRequest<MedicalReportDto>;

public class AddMedicalReportCommandValidator : AbstractValidator<AddMedicalReportCommand>
{
    public AddMedicalReportCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public sealed class AddMedicalReportCommandHandler
    : IRequestHandler<AddMedicalReportCommand, MedicalReportDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddMedicalReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MedicalReportDto> Handle(AddMedicalReportCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var report = new MedicalReport
        {
            CorporationId = student.CorporationId,
            StudentId     = req.StudentId,
            Title         = req.Title,
            ReportDate    = req.ReportDate,
            Issuer        = req.Issuer,
            Summary       = req.Summary,
            FileId        = req.FileId,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow,
            CreatedBy     = _currentUser.UserId
        };

        _db.MedicalReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToMedicalReportDto(report);
    }
}

// ── UpdateMedicalReportCommand ────────────────────────────────────────────────

public record UpdateMedicalReportCommand(
    Guid Id,
    string Title,
    DateOnly? ReportDate,
    string? Issuer,
    string? Summary,
    Guid? FileId,
    int RowVersion) : IRequest<MedicalReportDto>;

public class UpdateMedicalReportCommandValidator : AbstractValidator<UpdateMedicalReportCommand>
{
    public UpdateMedicalReportCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateMedicalReportCommandHandler
    : IRequestHandler<UpdateMedicalReportCommand, MedicalReportDto>
{
    private readonly IAppDbContext _db;

    public UpdateMedicalReportCommandHandler(IAppDbContext db) => _db = db;

    public async Task<MedicalReportDto> Handle(UpdateMedicalReportCommand req, CancellationToken ct)
    {
        var report = await _db.MedicalReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Medical report {req.Id} not found.");

        if (report.RowVersion != req.RowVersion)
            throw new InvalidOperationException("The report record was modified by another user. Please refresh and retry.");

        report.Title      = req.Title;
        report.ReportDate = req.ReportDate;
        report.Issuer     = req.Issuer;
        report.Summary    = req.Summary;
        report.FileId     = req.FileId;
        report.UpdatedAt  = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToMedicalReportDto(report);
    }
}

// ── DeleteMedicalReportCommand ────────────────────────────────────────────────

public record DeleteMedicalReportCommand(Guid Id) : IRequest;

public sealed class DeleteMedicalReportCommandHandler : IRequestHandler<DeleteMedicalReportCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteMedicalReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteMedicalReportCommand req, CancellationToken ct)
    {
        var report = await _db.MedicalReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Medical report {req.Id} not found.");

        report.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
