using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Commands;

// ── RecordAttendanceCommand ───────────────────────────────────────────────────

public record RecordAttendanceCommand(
    Guid SessionId,
    Guid StudentId,
    string Status,
    Guid? ReasonId,
    int? MinutesAttended,
    string? Note) : IRequest<AttendanceDto>;

public class RecordAttendanceCommandValidator : AbstractValidator<RecordAttendanceCommand>
{
    private static readonly string[] ValidStatuses =
        ["present", "absent", "late", "excused", "left_early"];

    public RecordAttendanceCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be present, absent, late, excused, or left_early.");
        RuleFor(x => x.MinutesAttended).GreaterThanOrEqualTo(0).When(x => x.MinutesAttended.HasValue);
    }
}

public sealed class RecordAttendanceCommandHandler : IRequestHandler<RecordAttendanceCommand, AttendanceDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecordAttendanceCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AttendanceDto> Handle(RecordAttendanceCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {req.SessionId} not found.");

        var existing = await _db.Attendances
            .FirstOrDefaultAsync(
                a => a.SessionId == req.SessionId && a.StudentId == req.StudentId, ct);

        string studentName;
        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == req.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);
        studentName = student is null ? "" : $"{student.FirstName} {student.LastName}".Trim();

        Attendance attendance;

        if (existing is not null)
        {
            existing.Correct(req.Status, req.ReasonId, req.MinutesAttended, req.Note, _currentUser.UserId);
            attendance = existing;
        }
        else
        {
            attendance = Attendance.Record(
                session.CorporationId, req.SessionId, req.StudentId,
                req.Status, req.ReasonId, req.MinutesAttended, req.Note,
                _currentUser.UserId);
            _db.Attendances.Add(attendance);
        }

        await _db.SaveChangesAsync(ct);

        return new AttendanceDto(
            attendance.Id, attendance.SessionId, attendance.StudentId, studentName,
            attendance.Status, attendance.ReasonId, attendance.MinutesAttended,
            attendance.Note, attendance.RecordedBy, attendance.RecordedAt);
    }
}

// ── BulkRecordAttendanceCommand ───────────────────────────────────────────────

public record BulkAttendanceEntry(
    Guid StudentId,
    string Status,
    Guid? ReasonId,
    int? MinutesAttended,
    string? Note);

public record BulkRecordAttendanceCommand(
    Guid SessionId,
    List<BulkAttendanceEntry> Entries) : IRequest<IReadOnlyList<AttendanceDto>>;

public class BulkRecordAttendanceCommandValidator : AbstractValidator<BulkRecordAttendanceCommand>
{
    private static readonly string[] ValidStatuses =
        ["present", "absent", "late", "excused", "left_early"];

    public BulkRecordAttendanceCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Entries).NotEmpty();
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.StudentId).NotEmpty();
            entry.RuleFor(e => e.Status)
                .Must(s => ValidStatuses.Contains(s))
                .WithMessage("Status must be present, absent, late, excused, or left_early.");
        });
    }
}

public sealed class BulkRecordAttendanceCommandHandler
    : IRequestHandler<BulkRecordAttendanceCommand, IReadOnlyList<AttendanceDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BulkRecordAttendanceCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AttendanceDto>> Handle(
        BulkRecordAttendanceCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {req.SessionId} not found.");

        var studentIds = req.Entries.Select(e => e.StudentId).ToList();

        var existingMap = await _db.Attendances
            .Where(a => a.SessionId == req.SessionId && studentIds.Contains(a.StudentId))
            .ToDictionaryAsync(a => a.StudentId, ct);

        var studentNames = await _db.Students.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => new { s.Id, s.FirstName, s.LastName })
            .ToDictionaryAsync(s => s.Id, s => $"{s.FirstName} {s.LastName}".Trim(), ct);

        var results = new List<AttendanceDto>();

        foreach (var entry in req.Entries)
        {
            Attendance attendance;

            if (existingMap.TryGetValue(entry.StudentId, out var existing))
            {
                existing.Correct(entry.Status, entry.ReasonId,
                    entry.MinutesAttended, entry.Note, _currentUser.UserId);
                attendance = existing;
            }
            else
            {
                attendance = Attendance.Record(
                    session.CorporationId, req.SessionId, entry.StudentId,
                    entry.Status, entry.ReasonId, entry.MinutesAttended,
                    entry.Note, _currentUser.UserId);
                _db.Attendances.Add(attendance);
            }

            studentNames.TryGetValue(entry.StudentId, out var name);
            results.Add(new AttendanceDto(
                attendance.Id, attendance.SessionId, attendance.StudentId, name ?? "",
                attendance.Status, attendance.ReasonId, attendance.MinutesAttended,
                attendance.Note, attendance.RecordedBy, attendance.RecordedAt));
        }

        await _db.SaveChangesAsync(ct);
        return results;
    }
}
