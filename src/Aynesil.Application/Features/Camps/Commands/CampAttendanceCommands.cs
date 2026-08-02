using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── RecordAttendanceCommand ───────────────────────────────────────────────────

public record RecordAttendanceCommand(
    Guid CorporationId,
    Guid CampEnrollmentId,
    DateOnly AttendanceDate,
    string Status,
    Guid? ReasonId = null,
    Guid? RecordedBy = null) : IRequest<CampAttendanceDto>;

public class RecordAttendanceCommandValidator : AbstractValidator<RecordAttendanceCommand>
{
    private static readonly string[] ValidStatuses = ["present", "absent", "late", "excused"];

    public RecordAttendanceCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CampEnrollmentId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be: present, absent, late, or excused.");
    }
}

public sealed class RecordAttendanceCommandHandler
    : IRequestHandler<RecordAttendanceCommand, CampAttendanceDto>
{
    private readonly IAppDbContext _db;

    public RecordAttendanceCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CampAttendanceDto> Handle(RecordAttendanceCommand req, CancellationToken ct)
    {
        var enrollmentExists = await _db.CampEnrollments
            .AnyAsync(e => e.Id == req.CampEnrollmentId
                        && e.Status == "enrolled", ct);
        if (!enrollmentExists)
            throw new InvalidOperationException(
                $"Enrollment {req.CampEnrollmentId} not found or student is not actively enrolled.");

        var existing = await _db.CampAttendances
            .FirstOrDefaultAsync(a => a.CampEnrollmentId == req.CampEnrollmentId
                                   && a.AttendanceDate == req.AttendanceDate, ct);

        if (existing != null)
            throw new InvalidOperationException(
                $"Attendance for {req.AttendanceDate:yyyy-MM-dd} is already recorded. Use the update endpoint.");

        var attendance = CampAttendance.Record(
            req.CorporationId, req.CampEnrollmentId,
            req.AttendanceDate, req.Status,
            req.ReasonId, req.RecordedBy);

        _db.CampAttendances.Add(attendance);
        await _db.SaveChangesAsync(ct);

        return new CampAttendanceDto(
            attendance.Id, attendance.CampEnrollmentId,
            attendance.AttendanceDate, attendance.Status,
            attendance.ReasonId, attendance.RecordedBy);
    }
}

// ── UpdateAttendanceCommand ───────────────────────────────────────────────────

public record UpdateAttendanceCommand(
    Guid AttendanceId,
    string Status,
    Guid? ReasonId,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateAttendanceCommandValidator : AbstractValidator<UpdateAttendanceCommand>
{
    private static readonly string[] ValidStatuses = ["present", "absent", "late", "excused"];

    public UpdateAttendanceCommandValidator()
    {
        RuleFor(x => x.AttendanceId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be: present, absent, late, or excused.");
    }
}

public sealed class UpdateAttendanceCommandHandler : IRequestHandler<UpdateAttendanceCommand>
{
    private readonly IAppDbContext _db;

    public UpdateAttendanceCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateAttendanceCommand req, CancellationToken ct)
    {
        var attendance = await _db.CampAttendances
            .FirstOrDefaultAsync(a => a.Id == req.AttendanceId, ct)
            ?? throw new KeyNotFoundException($"Attendance record {req.AttendanceId} not found.");

        attendance.Update(req.Status, req.ReasonId, req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── BulkRecordAttendanceCommand ───────────────────────────────────────────────

/// <summary>
/// Records attendance for all enrolled students in a camp period on a given date.
/// Skips students who already have a record for that date.
/// </summary>
public record BulkAttendanceInput(Guid EnrollmentId, string Status, Guid? ReasonId);

public record BulkRecordAttendanceCommand(
    Guid CorporationId,
    Guid CampPeriodId,
    DateOnly AttendanceDate,
    IReadOnlyList<BulkAttendanceInput> Records,
    Guid? RecordedBy = null) : IRequest<int>;

public class BulkRecordAttendanceCommandValidator
    : AbstractValidator<BulkRecordAttendanceCommand>
{
    private static readonly string[] ValidStatuses = ["present", "absent", "late", "excused"];

    public BulkRecordAttendanceCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CampPeriodId).NotEmpty();
        RuleFor(x => x.Records).NotEmpty().WithMessage("At least one attendance record is required.");
        RuleForEach(x => x.Records).ChildRules(r =>
        {
            r.RuleFor(x => x.EnrollmentId).NotEmpty();
            r.RuleFor(x => x.Status)
                .Must(s => ValidStatuses.Contains(s))
                .WithMessage("Status must be: present, absent, late, or excused.");
        });
    }
}

public sealed class BulkRecordAttendanceCommandHandler
    : IRequestHandler<BulkRecordAttendanceCommand, int>
{
    private readonly IAppDbContext _db;

    public BulkRecordAttendanceCommandHandler(IAppDbContext db) => _db = db;

    public async Task<int> Handle(BulkRecordAttendanceCommand req, CancellationToken ct)
    {
        var enrollmentIds = req.Records.Select(r => r.EnrollmentId).ToList();

        var existingDates = await _db.CampAttendances
            .Where(a => enrollmentIds.Contains(a.CampEnrollmentId)
                     && a.AttendanceDate == req.AttendanceDate)
            .Select(a => a.CampEnrollmentId)
            .ToHashSetAsync(ct);

        var added = 0;
        foreach (var record in req.Records)
        {
            if (existingDates.Contains(record.EnrollmentId))
                continue;

            _db.CampAttendances.Add(CampAttendance.Record(
                req.CorporationId, record.EnrollmentId,
                req.AttendanceDate, record.Status,
                record.ReasonId, req.RecordedBy));
            added++;
        }

        if (added > 0)
            await _db.SaveChangesAsync(ct);

        return added;
    }
}
