using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
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
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Student"));

        var report = new DevelopmentReport
        {
            CorporationId = student.CorporationId,
            StudentId     = req.StudentId,
            PeriodLabel   = req.PeriodLabel,
            ReportDate    = req.ReportDate,
            AuthoredBy    = _currentUser.UserId,
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
    private readonly ICurrentUserService _currentUser;

    public UpdateDevelopmentReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DevelopmentReportDto> Handle(UpdateDevelopmentReportCommand req, CancellationToken ct)
    {
        var report = await _db.DevelopmentReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "DevelopmentReport"));

        if (report.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(MsgKeys.Error.Concurrency);

        report.EnsureEditable();

        report.PeriodLabel = req.PeriodLabel;
        report.ReportDate  = req.ReportDate;
        // AuthoredBy is set once at creation; editing does not change the author.
        report.Content     = req.Content;
        report.FileId      = req.FileId;
        report.UpdatedAt   = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToDevelopmentReportDto(report);
    }
}

// ── FinalizeDevelopmentReportCommand (Y15) ───────────────────────────────────

/// <summary>Draft → finalized. Only finalized reports are visible to guardians in the parent portal.</summary>
public record FinalizeDevelopmentReportCommand(Guid Id, int RowVersion) : IRequest<DevelopmentReportDto>;

public class FinalizeDevelopmentReportCommandValidator : AbstractValidator<FinalizeDevelopmentReportCommand>
{
    public FinalizeDevelopmentReportCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class FinalizeDevelopmentReportCommandHandler
    : IRequestHandler<FinalizeDevelopmentReportCommand, DevelopmentReportDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FinalizeDevelopmentReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DevelopmentReportDto> Handle(FinalizeDevelopmentReportCommand req, CancellationToken ct)
    {
        var report = await _db.DevelopmentReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "DevelopmentReport"));

        if (report.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(MsgKeys.Error.Concurrency);

        report.Finalize(_currentUser.UserId);
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
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "DevelopmentReport"));

        report.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
