using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── AddDevelopmentReportCommand ───────────────────────────────────────────────

public record AddDevelopmentReportCommand(
    Guid StudentId,
    string? PeriodLabel,
    DateOnly? ReportDate,
    Guid? AuthoredBy,
    string? Content,
    Guid? FileId) : IRequest<DevelopmentReportDto>;

public class AddDevelopmentReportCommandValidator
    : AbstractValidator<AddDevelopmentReportCommand>
{
    public AddDevelopmentReportCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.PeriodLabel).MaximumLength(100).When(x => x.PeriodLabel is not null);
    }
}

public sealed class AddDevelopmentReportCommandHandler
    : IRequestHandler<AddDevelopmentReportCommand, DevelopmentReportDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddDevelopmentReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DevelopmentReportDto> Handle(AddDevelopmentReportCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var report = new DevelopmentReport
        {
            CorporationId = student.CorporationId,
            StudentId     = req.StudentId,
            PeriodLabel   = req.PeriodLabel,
            ReportDate    = req.ReportDate,
            AuthoredBy    = req.AuthoredBy ?? _currentUser.UserId,
            Content       = req.Content,
            FileId        = req.FileId,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow,
            CreatedBy     = _currentUser.UserId
        };

        _db.DevelopmentReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToDevelopmentReportDto(report);
    }
}

// ── UpdateDevelopmentReportCommand ───────────────────────────────────────────

public record UpdateDevelopmentReportCommand(
    Guid Id,
    string? PeriodLabel,
    DateOnly? ReportDate,
    Guid? AuthoredBy,
    string? Content,
    Guid? FileId,
    int RowVersion) : IRequest<DevelopmentReportDto>;

public class UpdateDevelopmentReportCommandValidator
    : AbstractValidator<UpdateDevelopmentReportCommand>
{
    public UpdateDevelopmentReportCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PeriodLabel).MaximumLength(100).When(x => x.PeriodLabel is not null);
    }
}

public sealed class UpdateDevelopmentReportCommandHandler
    : IRequestHandler<UpdateDevelopmentReportCommand, DevelopmentReportDto>
{
    private readonly IAppDbContext _db;

    public UpdateDevelopmentReportCommandHandler(IAppDbContext db) => _db = db;

    public async Task<DevelopmentReportDto> Handle(UpdateDevelopmentReportCommand req, CancellationToken ct)
    {
        var report = await _db.DevelopmentReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Development report {req.Id} not found.");

        if (report.RowVersion != req.RowVersion)
            throw new InvalidOperationException("The report record was modified by another user. Please refresh and retry.");

        report.PeriodLabel = req.PeriodLabel;
        report.ReportDate  = req.ReportDate;
        report.AuthoredBy  = req.AuthoredBy ?? report.AuthoredBy;
        report.Content     = req.Content;
        report.FileId      = req.FileId;
        report.UpdatedAt   = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToDevelopmentReportDto(report);
    }
}

// ── DeleteDevelopmentReportCommand ───────────────────────────────────────────

public record DeleteDevelopmentReportCommand(Guid Id) : IRequest;

public sealed class DeleteDevelopmentReportCommandHandler
    : IRequestHandler<DeleteDevelopmentReportCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteDevelopmentReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteDevelopmentReportCommand req, CancellationToken ct)
    {
        var report = await _db.DevelopmentReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Development report {req.Id} not found.");

        report.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
