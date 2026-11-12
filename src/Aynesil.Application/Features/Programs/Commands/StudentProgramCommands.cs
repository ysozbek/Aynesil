using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Programs.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Programs.Commands;

// ── AssignStudentToProgramCommand ─────────────────────────────────────────────

public record AssignStudentToProgramCommand(
    Guid StudentId,
    Guid ProgramId,
    Guid? EnrollmentId,
    Guid? CampusId,
    DateOnly? StartDate,
    DateOnly? EndDate) : IRequest<StudentProgramDto>;

public class AssignStudentToProgramCommandValidator : AbstractValidator<AssignStudentToProgramCommand>
{
    public AssignStudentToProgramCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.ProgramId).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithKey(MsgKeys.Validation.MustBeAfter, Msg.FieldArg("EndDate"), Msg.FieldArg("StartDate"));
    }
}

public sealed class AssignStudentToProgramCommandHandler
    : IRequestHandler<AssignStudentToProgramCommand, StudentProgramDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AssignStudentToProgramCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<StudentProgramDto> Handle(AssignStudentToProgramCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var studentExists = await _db.Students.AnyAsync(s => s.CorporationId == corporationId && s.Id == req.StudentId, ct);
        if (!studentExists)
            throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var programExists = await _db.EducationPrograms.AnyAsync(
            p => p.CorporationId == corporationId && p.Id == req.ProgramId && p.IsActive, ct);
        if (!programExists)
            throw new KeyNotFoundException($"Active program {req.ProgramId} not found.");

        if (req.EnrollmentId.HasValue)
        {
            var enrollmentExists = await _db.Enrollments.AnyAsync(
                e => e.CorporationId == corporationId && e.Id == req.EnrollmentId.Value && e.StudentId == req.StudentId, ct);
            if (!enrollmentExists)
                throw new KeyNotFoundException(
                    $"Enrollment {req.EnrollmentId} not found for student {req.StudentId}.");
        }

        var sp = StudentProgram.Create(
            corporationId, req.StudentId, req.ProgramId,
            req.EnrollmentId, req.CampusId,
            req.StartDate, req.EndDate);

        _db.StudentPrograms.Add(sp);
        await _db.SaveChangesAsync(ct);

        var program = await _db.EducationPrograms.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CorporationId == corporationId && p.Id == req.ProgramId, ct);
        var campusName = sp.CampusId.HasValue
            ? await _db.Campuses.AsNoTracking()
                .Where(c => c.Id == sp.CampusId.Value)
                .Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;

        return ProgramProjection.ToStudentProgramDto(sp, program?.Name, program?.Code, campusName);
    }
}

// ── UpdateStudentProgramCommand ───────────────────────────────────────────────

public record UpdateStudentProgramCommand(
    Guid StudentProgramId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Status,
    int RowVersion) : IRequest<StudentProgramDto>;

public class UpdateStudentProgramCommandValidator : AbstractValidator<UpdateStudentProgramCommand>
{
    public UpdateStudentProgramCommandValidator()
    {
        RuleFor(x => x.StudentProgramId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => new[] { "active", "paused", "completed", "cancelled" }.Contains(s))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Status"), EnumSets.ProgramStatus);
    }
}

public sealed class UpdateStudentProgramCommandHandler
    : IRequestHandler<UpdateStudentProgramCommand, StudentProgramDto>
{
    private readonly IAppDbContext _db;

    public UpdateStudentProgramCommandHandler(IAppDbContext db) => _db = db;

    public async Task<StudentProgramDto> Handle(UpdateStudentProgramCommand req, CancellationToken ct)
    {
        var sp = await _db.StudentPrograms.FirstOrDefaultAsync(s => s.Id == req.StudentProgramId, ct)
            ?? throw new KeyNotFoundException($"StudentProgram {req.StudentProgramId} not found.");

        if (sp.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(MsgKeys.Error.Concurrency);

        sp.UpdateDates(req.StartDate, req.EndDate);
        sp.ChangeStatus(req.Status);

        await _db.SaveChangesAsync(ct);

        var program = await _db.EducationPrograms.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == sp.ProgramId, ct);
        var campusName = sp.CampusId.HasValue
            ? await _db.Campuses.AsNoTracking()
                .Where(c => c.Id == sp.CampusId.Value)
                .Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;

        return ProgramProjection.ToStudentProgramDto(sp, program?.Name, program?.Code, campusName);
    }
}

// ── RemoveStudentFromProgramCommand ───────────────────────────────────────────

public record RemoveStudentFromProgramCommand(Guid StudentProgramId) : IRequest;

public sealed class RemoveStudentFromProgramCommandHandler
    : IRequestHandler<RemoveStudentFromProgramCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RemoveStudentFromProgramCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveStudentFromProgramCommand req, CancellationToken ct)
    {
        var sp = await _db.StudentPrograms.FirstOrDefaultAsync(s => s.Id == req.StudentProgramId, ct)
            ?? throw new KeyNotFoundException($"StudentProgram {req.StudentProgramId} not found.");

        sp.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
