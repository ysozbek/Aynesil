using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── RecordAttendanceCommand ───────────────────────────────────────────────────

public record RecordAttendanceCommand(
    Guid CampEnrollmentId,
    DateOnly AttendanceDate,
    string Status,
    Guid? ReasonId = null) : IRequest<CampAttendanceDto>;

public class RecordAttendanceCommandValidator : AbstractValidator<RecordAttendanceCommand>
{
    private static readonly string[] ValidStatuses = ["present", "absent", "late", "excused"];

    public RecordAttendanceCommandValidator()
    {
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
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public RecordAttendanceCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<CampAttendanceDto> Handle(RecordAttendanceCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var enrollmentExists = await _db.CampEnrollments
            .AnyAsync(e => e.CorporationId == corporationId && e.Id == req.CampEnrollmentId
                        && e.Status == "enrolled", ct);
        if (!enrollmentExists)
            throw new BusinessRuleViolationException(
                $"Enrollment {req.CampEnrollmentId} not found or student is not actively enrolled.");

        var existing = await _db.CampAttendances
            .FirstOrDefaultAsync(a => a.CorporationId == corporationId && a.CampEnrollmentId == req.CampEnrollmentId
                                   && a.AttendanceDate == req.AttendanceDate, ct);

        if (existing != null)
            throw new BusinessRuleViolationException(
                $"Attendance for {req.AttendanceDate:yyyy-MM-dd} is already recorded. Use the update endpoint.");

        var attendance = CampAttendance.Record(
            corporationId, req.CampEnrollmentId,
            req.AttendanceDate, req.Status,
            req.ReasonId, _currentUser.UserId);

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
    Guid? ReasonId) : IRequest;

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
    private readonly ICurrentUserService _currentUser;

    public UpdateAttendanceCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAttendanceCommand req, CancellationToken ct)
    {
        var attendance = await _db.CampAttendances
            .FirstOrDefaultAsync(a => a.Id == req.AttendanceId, ct)
            ?? throw new KeyNotFoundException($"Attendance record {req.AttendanceId} not found.");

        attendance.Update(req.Status, req.ReasonId, _currentUser.UserId);
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
    Guid CampPeriodId,
    DateOnly AttendanceDate,
    IReadOnlyList<BulkAttendanceInput> Records) : IRequest<int>;

public class BulkRecordAttendanceCommandValidator
    : AbstractValidator<BulkRecordAttendanceCommand>
{
    private static readonly string[] ValidStatuses = ["present", "absent", "late", "excused"];

    public BulkRecordAttendanceCommandValidator()
    {
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
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public BulkRecordAttendanceCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<int> Handle(BulkRecordAttendanceCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var periodExists = await _db.CampPeriods
            .AnyAsync(p => p.Id == req.CampPeriodId && p.CorporationId == corporationId, ct);
        if (!periodExists)
            throw new KeyNotFoundException($"Camp period {req.CampPeriodId} not found.");

        var enrollmentIds = req.Records.Select(r => r.EnrollmentId).Distinct().ToList();

        // Every enrollment must belong to this period (and therefore to this tenant).
        var validCount = await _db.CampEnrollments
            .CountAsync(e => enrollmentIds.Contains(e.Id)
                          && e.CampPeriodId == req.CampPeriodId
                          && e.CorporationId == corporationId, ct);
        if (validCount != enrollmentIds.Count)
            throw new KeyNotFoundException("One or more enrollments do not belong to this camp period.");

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
                corporationId, record.EnrollmentId,
                req.AttendanceDate, record.Status,
                record.ReasonId, _currentUser.UserId));
            added++;
        }

        if (added > 0)
            await _db.SaveChangesAsync(ct);

        return added;
    }
}
