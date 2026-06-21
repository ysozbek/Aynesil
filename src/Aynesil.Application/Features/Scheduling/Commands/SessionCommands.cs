using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Commands;

// ── ScheduleSessionCommand ────────────────────────────────────────────────────

public record ScheduleSessionCommand(
    Guid CorporationId,
    Guid SessionTypeId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid? CampusId,
    Guid? RoomId,
    Guid? RecurringScheduleId,
    Guid? ProgramServiceId,
    string? Title,
    bool IsMakeup,
    List<Guid> ParticipantStudentIds,
    List<EducatorAssignment> EducatorAssignments) : IRequest<SessionDto>;

public record EducatorAssignment(Guid EducatorId, string Role = "lead");

public class ScheduleSessionCommandValidator : AbstractValidator<ScheduleSessionCommand>
{
    public ScheduleSessionCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.SessionTypeId).NotEmpty();
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt)
            .WithMessage("Session end time must be after start time.");
        RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title != null);
    }
}

public sealed class ScheduleSessionCommandHandler : IRequestHandler<ScheduleSessionCommand, SessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ISessionRepository _sessions;
    private readonly ICurrentUserService _currentUser;

    public ScheduleSessionCommandHandler(
        IAppDbContext db, ISessionRepository sessions, ICurrentUserService currentUser)
    {
        _db = db;
        _sessions = sessions;
        _currentUser = currentUser;
    }

    public async Task<SessionDto> Handle(ScheduleSessionCommand req, CancellationToken ct)
    {
        // Conflict detection
        if (req.RoomId.HasValue)
        {
            var roomConflict = await _sessions.HasRoomConflictAsync(
                req.RoomId.Value, req.StartsAt, req.EndsAt, null, ct);
            if (roomConflict)
                throw new InvalidOperationException(
                    $"Room {req.RoomId} is already booked for the requested time slot.");
        }

        foreach (var ea in req.EducatorAssignments)
        {
            var educatorConflict = await _sessions.HasEducatorConflictAsync(
                ea.EducatorId, req.StartsAt, req.EndsAt, null, ct);
            if (educatorConflict)
                throw new InvalidOperationException(
                    $"Educator {ea.EducatorId} is already booked for the requested time slot.");
        }

        var session = Session.Schedule(
            req.CorporationId, req.SessionTypeId,
            req.StartsAt, req.EndsAt,
            req.CampusId, req.RoomId, req.RecurringScheduleId,
            req.ProgramServiceId, req.Title, req.IsMakeup,
            _currentUser.UserId);

        foreach (var studentId in req.ParticipantStudentIds)
            session.Participants.Add(
                SessionParticipant.Create(req.CorporationId, session.Id, studentId));

        foreach (var ea in req.EducatorAssignments)
            session.Educators.Add(
                SessionEducator.Assign(req.CorporationId, session.Id, ea.EducatorId, ea.Role));

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return (await SchedulingProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }
}

// ── RescheduleSessionCommand ──────────────────────────────────────────────────

public record RescheduleSessionCommand(
    Guid Id,
    DateTimeOffset NewStartsAt,
    DateTimeOffset NewEndsAt,
    Guid? RoomId,
    int RowVersion) : IRequest<SessionDto>;

public class RescheduleSessionCommandValidator : AbstractValidator<RescheduleSessionCommand>
{
    public RescheduleSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewEndsAt).GreaterThan(x => x.NewStartsAt)
            .WithMessage("Session end time must be after start time.");
    }
}

public sealed class RescheduleSessionCommandHandler : IRequestHandler<RescheduleSessionCommand, SessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ISessionRepository _sessions;
    private readonly ICurrentUserService _currentUser;

    public RescheduleSessionCommandHandler(
        IAppDbContext db, ISessionRepository sessions, ICurrentUserService currentUser)
    {
        _db = db;
        _sessions = sessions;
        _currentUser = currentUser;
    }

    public async Task<SessionDto> Handle(RescheduleSessionCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Session {req.Id} not found.");

        if (session.RowVersion != req.RowVersion)
            throw new InvalidOperationException("Session was modified by another user. Please refresh and retry.");

        if (req.RoomId.HasValue)
        {
            var conflict = await _sessions.HasRoomConflictAsync(
                req.RoomId.Value, req.NewStartsAt, req.NewEndsAt, req.Id, ct);
            if (conflict)
                throw new InvalidOperationException(
                    $"Room {req.RoomId} is already booked for the new time slot.");
        }

        session.Reschedule(req.NewStartsAt, req.NewEndsAt, req.RoomId, _currentUser.UserId);

        await _db.SaveChangesAsync(ct);
        return (await SchedulingProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }
}

// ── CompleteSessionCommand ────────────────────────────────────────────────────

public record CompleteSessionCommand(Guid Id, int RowVersion) : IRequest<SessionDto>;

public sealed class CompleteSessionCommandHandler : IRequestHandler<CompleteSessionCommand, SessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CompleteSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SessionDto> Handle(CompleteSessionCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Session {req.Id} not found.");

        if (session.RowVersion != req.RowVersion)
            throw new InvalidOperationException("Session was modified by another user. Please refresh and retry.");

        session.Complete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await SchedulingProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }
}

// ── CancelSessionCommand ──────────────────────────────────────────────────────

public record CancelSessionCommand(Guid Id, string? Reason, int RowVersion) : IRequest<SessionDto>;

public sealed class CancelSessionCommandHandler : IRequestHandler<CancelSessionCommand, SessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SessionDto> Handle(CancelSessionCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Session {req.Id} not found.");

        if (session.RowVersion != req.RowVersion)
            throw new InvalidOperationException("Session was modified by another user. Please refresh and retry.");

        session.Cancel(req.Reason, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await SchedulingProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }
}

// ── MarkSessionNoShowCommand ──────────────────────────────────────────────────

public record MarkSessionNoShowCommand(Guid Id, int RowVersion) : IRequest<SessionDto>;

public sealed class MarkSessionNoShowCommandHandler : IRequestHandler<MarkSessionNoShowCommand, SessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MarkSessionNoShowCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SessionDto> Handle(MarkSessionNoShowCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Session {req.Id} not found.");

        if (session.RowVersion != req.RowVersion)
            throw new InvalidOperationException("Session was modified by another user. Please refresh and retry.");

        session.MarkNoShow(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await SchedulingProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }
}

// ── AddSessionParticipantCommand ──────────────────────────────────────────────

public record AddSessionParticipantCommand(
    Guid SessionId,
    Guid StudentId,
    Guid? StudentProgramId,
    string Role = "student") : IRequest<SessionParticipantDto>;

public sealed class AddSessionParticipantCommandHandler
    : IRequestHandler<AddSessionParticipantCommand, SessionParticipantDto>
{
    private readonly IAppDbContext _db;

    public AddSessionParticipantCommandHandler(IAppDbContext db) => _db = db;

    public async Task<SessionParticipantDto> Handle(AddSessionParticipantCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {req.SessionId} not found.");

        if (session.Participants.Any(p => p.StudentId == req.StudentId))
            throw new InvalidOperationException("Student is already a participant in this session.");

        var participant = SessionParticipant.Create(
            session.CorporationId, req.SessionId, req.StudentId, req.StudentProgramId, req.Role);

        _db.SessionParticipants.Add(participant);
        await _db.SaveChangesAsync(ct);

        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == req.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);

        return new SessionParticipantDto(
            participant.Id, req.SessionId, req.StudentId,
            student is null ? "" : $"{student.FirstName} {student.LastName}".Trim(),
            req.StudentProgramId, req.Role);
    }
}

// ── RemoveSessionParticipantCommand ───────────────────────────────────────────

public record RemoveSessionParticipantCommand(Guid SessionId, Guid StudentId) : IRequest;

public sealed class RemoveSessionParticipantCommandHandler
    : IRequestHandler<RemoveSessionParticipantCommand>
{
    private readonly IAppDbContext _db;

    public RemoveSessionParticipantCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveSessionParticipantCommand req, CancellationToken ct)
    {
        var participant = await _db.SessionParticipants
            .FirstOrDefaultAsync(
                p => p.SessionId == req.SessionId && p.StudentId == req.StudentId, ct)
            ?? throw new KeyNotFoundException("Participant not found in session.");

        _db.SessionParticipants.Remove(participant);
        await _db.SaveChangesAsync(ct);
    }
}

// ── AssignSessionEducatorCommand ──────────────────────────────────────────────

public record AssignSessionEducatorCommand(
    Guid SessionId,
    Guid EducatorId,
    string Role = "lead") : IRequest<SessionEducatorDto>;

public sealed class AssignSessionEducatorCommandHandler
    : IRequestHandler<AssignSessionEducatorCommand, SessionEducatorDto>
{
    private readonly IAppDbContext _db;
    private readonly ISessionRepository _sessions;

    public AssignSessionEducatorCommandHandler(IAppDbContext db, ISessionRepository sessions)
    {
        _db = db;
        _sessions = sessions;
    }

    public async Task<SessionEducatorDto> Handle(AssignSessionEducatorCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions
            .Include(s => s.Educators)
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {req.SessionId} not found.");

        if (session.Educators.Any(e => e.EducatorId == req.EducatorId))
            throw new InvalidOperationException("Educator is already assigned to this session.");

        var conflict = await _sessions.HasEducatorConflictAsync(
            req.EducatorId, session.StartsAt, session.EndsAt, session.Id, ct);
        if (conflict)
            throw new InvalidOperationException(
                $"Educator {req.EducatorId} is already booked in an overlapping session.");

        var assignment = SessionEducator.Assign(
            session.CorporationId, req.SessionId, req.EducatorId, req.Role);

        _db.SessionEducators.Add(assignment);
        await _db.SaveChangesAsync(ct);

        var educator = await _db.Educators.AsNoTracking()
            .Where(e => e.Id == req.EducatorId)
            .Select(e => new { e.FirstName, e.LastName })
            .FirstOrDefaultAsync(ct);

        return new SessionEducatorDto(
            assignment.Id, req.SessionId, req.EducatorId,
            educator is null ? "" : $"{educator.FirstName} {educator.LastName}".Trim(),
            req.Role);
    }
}

// ── RemoveSessionEducatorCommand ──────────────────────────────────────────────

public record RemoveSessionEducatorCommand(Guid SessionId, Guid EducatorId) : IRequest;

public sealed class RemoveSessionEducatorCommandHandler : IRequestHandler<RemoveSessionEducatorCommand>
{
    private readonly IAppDbContext _db;

    public RemoveSessionEducatorCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveSessionEducatorCommand req, CancellationToken ct)
    {
        var assignment = await _db.SessionEducators
            .FirstOrDefaultAsync(
                e => e.SessionId == req.SessionId && e.EducatorId == req.EducatorId, ct)
            ?? throw new KeyNotFoundException("Educator not assigned to this session.");

        _db.SessionEducators.Remove(assignment);
        await _db.SaveChangesAsync(ct);
    }
}

// ── UpsertSessionGoalCommand ──────────────────────────────────────────────────

public record UpsertSessionGoalCommand(
    Guid SessionId,
    Guid StudentGoalId,
    bool WorkedOn,
    string? ProgressNote,
    decimal? MeasuredValue) : IRequest<SessionGoalDto>;

public sealed class UpsertSessionGoalCommandHandler
    : IRequestHandler<UpsertSessionGoalCommand, SessionGoalDto>
{
    private readonly IAppDbContext _db;

    public UpsertSessionGoalCommandHandler(IAppDbContext db) => _db = db;

    public async Task<SessionGoalDto> Handle(UpsertSessionGoalCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {req.SessionId} not found.");

        var existing = await _db.SessionGoals
            .FirstOrDefaultAsync(
                g => g.SessionId == req.SessionId && g.StudentGoalId == req.StudentGoalId, ct);

        if (existing is not null)
        {
            existing.Update(req.WorkedOn, req.ProgressNote, req.MeasuredValue);
        }
        else
        {
            existing = SessionGoal.Create(
                session.CorporationId, req.SessionId, req.StudentGoalId,
                req.WorkedOn, req.ProgressNote, req.MeasuredValue);
            _db.SessionGoals.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        var statement = await _db.StudentGoals.AsNoTracking()
            .Where(g => g.Id == req.StudentGoalId)
            .Select(g => g.Statement)
            .FirstOrDefaultAsync(ct) ?? "";

        return new SessionGoalDto(
            existing.Id, req.SessionId, req.StudentGoalId, statement,
            existing.WorkedOn, existing.ProgressNote, existing.MeasuredValue);
    }
}

// ── RemoveSessionGoalCommand ──────────────────────────────────────────────────

public record RemoveSessionGoalCommand(Guid SessionId, Guid StudentGoalId) : IRequest;

public sealed class RemoveSessionGoalCommandHandler : IRequestHandler<RemoveSessionGoalCommand>
{
    private readonly IAppDbContext _db;

    public RemoveSessionGoalCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveSessionGoalCommand req, CancellationToken ct)
    {
        var goal = await _db.SessionGoals
            .FirstOrDefaultAsync(
                g => g.SessionId == req.SessionId && g.StudentGoalId == req.StudentGoalId, ct)
            ?? throw new KeyNotFoundException("Goal not found in session.");

        _db.SessionGoals.Remove(goal);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteSessionCommand ──────────────────────────────────────────────────────

public record DeleteSessionCommand(Guid Id) : IRequest;

public sealed class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteSessionCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Session {req.Id} not found.");

        session.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
