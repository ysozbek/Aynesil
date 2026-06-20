using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using Aynesil.Domain.Modules.Students.Events;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── EnrollStudentAtCampusCommand ──────────────────────────────────────────────

/// <summary>
/// Enrolls a student at a campus. Raises StudentEnrolledEvent.
/// If is_primary = true, the existing primary campus is not automatically closed —
/// call TransferStudentCommand instead when replacing the primary campus.
/// </summary>
public record EnrollStudentAtCampusCommand(
    Guid StudentId,
    Guid CampusId,
    bool IsPrimary,
    DateOnly? ActiveFrom) : IRequest<StudentCampusDto>;

public class EnrollStudentAtCampusCommandValidator
    : AbstractValidator<EnrollStudentAtCampusCommand>
{
    public EnrollStudentAtCampusCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.CampusId).NotEmpty();
    }
}

public sealed class EnrollStudentAtCampusCommandHandler
    : IRequestHandler<EnrollStudentAtCampusCommand, StudentCampusDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EnrollStudentAtCampusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StudentCampusDto> Handle(EnrollStudentAtCampusCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var duplicate = await _db.StudentCampuses
            .AnyAsync(sc => sc.StudentId == req.StudentId && sc.CampusId == req.CampusId && sc.ActiveTo == null, ct);
        if (duplicate)
            throw new InvalidOperationException("Student is already actively enrolled at this campus.");

        var enrollment = new StudentCampus
        {
            CorporationId = student.CorporationId,
            StudentId     = req.StudentId,
            CampusId      = req.CampusId,
            IsPrimary     = req.IsPrimary,
            ActiveFrom    = req.ActiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        enrollment.AddDomainEvent(new StudentEnrolledEvent(
            req.StudentId, student.CorporationId, req.CampusId, req.IsPrimary, _currentUser.UserId));

        _db.StudentCampuses.Add(enrollment);
        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToStudentCampusDto(enrollment);
    }
}

// ── TransferStudentCommand ────────────────────────────────────────────────────

/// <summary>
/// Transfers a student's primary campus. Closes the current primary enrollment
/// (sets active_to = today), creates a new primary enrollment at the target campus,
/// and raises StudentTransferredEvent.
/// </summary>
public record TransferStudentCommand(
    Guid StudentId,
    Guid NewCampusId,
    DateOnly? TransferDate,
    int RowVersion) : IRequest<StudentDto>;

public class TransferStudentCommandValidator : AbstractValidator<TransferStudentCommand>
{
    public TransferStudentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.NewCampusId).NotEmpty();
    }
}

public sealed class TransferStudentCommandHandler
    : IRequestHandler<TransferStudentCommand, StudentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TransferStudentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StudentDto> Handle(TransferStudentCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        if (student.RowVersion != req.RowVersion)
            throw new InvalidOperationException("The student record was modified by another user. Please refresh and retry.");

        if (student.PrimaryCampusId == req.NewCampusId)
            throw new InvalidOperationException("Student is already at this campus.");

        var transferDate = req.TransferDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var currentPrimary = await _db.StudentCampuses
            .FirstOrDefaultAsync(sc => sc.StudentId == req.StudentId && sc.IsPrimary && sc.ActiveTo == null, ct);

        if (currentPrimary is not null)
        {
            currentPrimary.ActiveTo   = transferDate.AddDays(-1);
            currentPrimary.IsPrimary  = false;
        }

        var previousCampusId = student.PrimaryCampusId;

        var newEnrollment = new StudentCampus
        {
            CorporationId = student.CorporationId,
            StudentId     = req.StudentId,
            CampusId      = req.NewCampusId,
            IsPrimary     = true,
            ActiveFrom    = transferDate
        };

        _db.StudentCampuses.Add(newEnrollment);

        student.UpdateProfile(student.FirstName, student.LastName, student.StudentNo,
            student.NationalId, student.BirthDate, student.Gender,
            req.NewCampusId, student.Notes, _currentUser.UserId);

        newEnrollment.AddDomainEvent(new StudentTransferredEvent(
            req.StudentId, student.CorporationId,
            previousCampusId ?? Guid.Empty, req.NewCampusId, _currentUser.UserId));

        await _db.SaveChangesAsync(ct);

        return (await StudentProjection.LoadStudentAsync(_db, student.Id, ct))!;
    }
}

// ── EndCampusEnrollmentCommand ────────────────────────────────────────────────

public record EndCampusEnrollmentCommand(Guid EnrollmentId, DateOnly? EndDate) : IRequest<StudentCampusDto>;

public sealed class EndCampusEnrollmentCommandHandler
    : IRequestHandler<EndCampusEnrollmentCommand, StudentCampusDto>
{
    private readonly IAppDbContext _db;

    public EndCampusEnrollmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<StudentCampusDto> Handle(EndCampusEnrollmentCommand req, CancellationToken ct)
    {
        var enrollment = await _db.StudentCampuses
            .FirstOrDefaultAsync(sc => sc.Id == req.EnrollmentId, ct)
            ?? throw new KeyNotFoundException($"Campus enrollment {req.EnrollmentId} not found.");

        if (enrollment.ActiveTo.HasValue)
            throw new InvalidOperationException("This enrollment has already been closed.");

        enrollment.ActiveTo = req.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToStudentCampusDto(enrollment);
    }
}
