using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Commands;

// ── CreateRecurringScheduleCommand ────────────────────────────────────────────

public record CreateRecurringScheduleCommand(
    Guid CorporationId,
    string Frequency,
    TimeOnly StartTime,
    int DurationMinutes,
    DateOnly RangeStart,
    Guid? CampusId,
    Guid? StudentProgramId,
    Guid? SessionTypeId,
    Guid? RoomId,
    int IntervalCount,
    int[]? ByWeekday,
    int[]? ByMonthday,
    DateOnly? RangeEnd,
    int? MaxOccurrences) : IRequest<RecurringScheduleDto>;

public class CreateRecurringScheduleCommandValidator
    : AbstractValidator<CreateRecurringScheduleCommand>
{
    public CreateRecurringScheduleCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Frequency)
            .Must(f => new[] { "weekly", "biweekly", "monthly" }.Contains(f))
            .WithMessage("Frequency must be weekly, biweekly, or monthly.");
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.RangeStart).NotEmpty();
        RuleFor(x => x.RangeEnd).GreaterThan(x => x.RangeStart)
            .When(x => x.RangeEnd.HasValue)
            .WithMessage("Range end must be after range start.");
        RuleFor(x => x.IntervalCount).GreaterThanOrEqualTo(1);
        RuleFor(x => x.MaxOccurrences).GreaterThan(0).When(x => x.MaxOccurrences.HasValue);
    }
}

public sealed class CreateRecurringScheduleCommandHandler
    : IRequestHandler<CreateRecurringScheduleCommand, RecurringScheduleDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateRecurringScheduleCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RecurringScheduleDto> Handle(
        CreateRecurringScheduleCommand req, CancellationToken ct)
    {
        var schedule = RecurringSchedule.Create(
            req.CorporationId, req.Frequency, req.StartTime, req.DurationMinutes,
            req.RangeStart, req.CampusId, req.StudentProgramId, req.SessionTypeId,
            req.RoomId, req.IntervalCount, req.ByWeekday, req.ByMonthday,
            req.RangeEnd, req.MaxOccurrences, _currentUser.UserId);

        _db.RecurringSchedules.Add(schedule);
        await _db.SaveChangesAsync(ct);

        return ToDto(schedule);
    }

    internal static RecurringScheduleDto ToDto(RecurringSchedule s)
        => new(s.Id, s.CorporationId, s.CampusId, s.StudentProgramId,
               s.SessionTypeId, s.RoomId, s.Frequency, s.IntervalCount,
               s.ByWeekday, s.ByMonthday, s.StartTime, s.DurationMinutes,
               s.RangeStart, s.RangeEnd, s.MaxOccurrences, s.IsActive,
               s.CreatedAt, s.RowVersion);
}

// ── DeactivateRecurringScheduleCommand ───────────────────────────────────────

public record DeactivateRecurringScheduleCommand(Guid Id) : IRequest;

public sealed class DeactivateRecurringScheduleCommandHandler
    : IRequestHandler<DeactivateRecurringScheduleCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeactivateRecurringScheduleCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeactivateRecurringScheduleCommand req, CancellationToken ct)
    {
        var schedule = await _db.RecurringSchedules
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"RecurringSchedule {req.Id} not found.");

        schedule.Deactivate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── AddRecurrenceExceptionCommand ─────────────────────────────────────────────

public record AddRecurrenceExceptionCommand(
    Guid RecurringScheduleId,
    DateOnly ExceptionDate,
    string Action,
    DateTimeOffset? NewStartAt,
    string? Reason) : IRequest;

public class AddRecurrenceExceptionCommandValidator
    : AbstractValidator<AddRecurrenceExceptionCommand>
{
    public AddRecurrenceExceptionCommandValidator()
    {
        RuleFor(x => x.RecurringScheduleId).NotEmpty();
        RuleFor(x => x.Action)
            .Must(a => new[] { "skip", "reschedule", "cancel" }.Contains(a))
            .WithMessage("Action must be skip, reschedule, or cancel.");
        RuleFor(x => x.NewStartAt).NotNull()
            .When(x => x.Action == "reschedule")
            .WithMessage("new_start_at is required when action is 'reschedule'.");
    }
}

public sealed class AddRecurrenceExceptionCommandHandler
    : IRequestHandler<AddRecurrenceExceptionCommand>
{
    private readonly IAppDbContext _db;

    public AddRecurrenceExceptionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(AddRecurrenceExceptionCommand req, CancellationToken ct)
    {
        var schedule = await _db.RecurringSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.RecurringScheduleId, ct)
            ?? throw new KeyNotFoundException($"RecurringSchedule {req.RecurringScheduleId} not found.");

        var duplicate = await _db.RecurrenceExceptions.AnyAsync(
            e => e.RecurringScheduleId == req.RecurringScheduleId
              && e.ExceptionDate == req.ExceptionDate, ct);
        if (duplicate)
            throw new InvalidOperationException(
                $"An exception already exists for {req.ExceptionDate} on this schedule.");

        var exception = RecurrenceException.Create(
            schedule.CorporationId, req.RecurringScheduleId,
            req.ExceptionDate, req.Action, req.NewStartAt, req.Reason);

        _db.RecurrenceExceptions.Add(exception);
        await _db.SaveChangesAsync(ct);
    }
}

// ── BulkGenerateSessionsCommand ───────────────────────────────────────────────

/// <summary>
/// Materialises concrete session rows from a recurring schedule rule for a given date window.
/// Skips occurrences that already have a session, fall on exception dates (skip/cancel),
/// or would cause a room conflict.
/// </summary>
public record BulkGenerateSessionsCommand(
    Guid RecurringScheduleId,
    DateOnly WindowStart,
    DateOnly WindowEnd) : IRequest<BulkOperationResultDto>;

public class BulkGenerateSessionsCommandValidator : AbstractValidator<BulkGenerateSessionsCommand>
{
    public BulkGenerateSessionsCommandValidator()
    {
        RuleFor(x => x.RecurringScheduleId).NotEmpty();
        RuleFor(x => x.WindowEnd).GreaterThan(x => x.WindowStart)
            .WithMessage("Window end must be after window start.");
    }
}

public sealed class BulkGenerateSessionsCommandHandler
    : IRequestHandler<BulkGenerateSessionsCommand, BulkOperationResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ISessionRepository _sessions;
    private readonly ICurrentUserService _currentUser;

    public BulkGenerateSessionsCommandHandler(
        IAppDbContext db, ISessionRepository sessions, ICurrentUserService currentUser)
    {
        _db = db;
        _sessions = sessions;
        _currentUser = currentUser;
    }

    public async Task<BulkOperationResultDto> Handle(
        BulkGenerateSessionsCommand req, CancellationToken ct)
    {
        var schedule = await _db.RecurringSchedules
            .AsNoTracking()
            .Include(s => s.Exceptions)
            .FirstOrDefaultAsync(s => s.Id == req.RecurringScheduleId, ct)
            ?? throw new KeyNotFoundException($"RecurringSchedule {req.RecurringScheduleId} not found.");

        if (!schedule.IsActive)
            throw new InvalidOperationException("Cannot generate sessions for an inactive recurring schedule.");

        // Load already-generated sessions for this window to avoid duplicates
        var windowStart = req.WindowStart.ToDateTime(schedule.StartTime, DateTimeKind.Utc);
        var windowEnd   = req.WindowEnd.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var existing = await _db.Sessions
            .Where(s => s.RecurringScheduleId == req.RecurringScheduleId
                     && s.StartsAt >= windowStart
                     && s.StartsAt <= windowEnd)
            .Select(s => DateOnly.FromDateTime(s.StartsAt.Date))
            .ToListAsync(ct);

        var existingDates = existing.ToHashSet();

        var exceptionMap = schedule.Exceptions
            .ToDictionary(e => e.ExceptionDate, e => e);

        // Expand occurrence dates
        var occurrences = ExpandOccurrences(schedule, req.WindowStart, req.WindowEnd);

        int created = 0;

        foreach (var date in occurrences)
        {
            // Skip already generated
            if (existingDates.Contains(date)) continue;

            // Handle exceptions
            DateTimeOffset startsAt;

            if (exceptionMap.TryGetValue(date, out var ex))
            {
                if (ex.Action is "skip" or "cancel") continue;
                if (ex.Action == "reschedule" && ex.NewStartAt.HasValue)
                    startsAt = ex.NewStartAt.Value;
                else continue;
            }
            else
            {
                startsAt = new DateTimeOffset(
                    date.Year, date.Month, date.Day,
                    schedule.StartTime.Hour, schedule.StartTime.Minute, 0,
                    TimeSpan.Zero);
            }

            var endsAt = startsAt.AddMinutes(schedule.DurationMinutes);

            // Room conflict check
            if (schedule.RoomId.HasValue)
            {
                var conflict = await _sessions.HasRoomConflictAsync(
                    schedule.RoomId.Value, startsAt, endsAt, null, ct);
                if (conflict) continue;
            }

            var session = Session.Schedule(
                schedule.CorporationId, schedule.SessionTypeId ?? Guid.Empty,
                startsAt, endsAt,
                schedule.CampusId, schedule.RoomId, schedule.Id,
                schedule.StudentProgramId, null, false,
                _currentUser.UserId);

            _db.Sessions.Add(session);
            created++;

            // Respect MaxOccurrences
            if (schedule.MaxOccurrences.HasValue &&
                existingDates.Count + created >= schedule.MaxOccurrences.Value)
                break;
        }

        if (created > 0)
            await _db.SaveChangesAsync(ct);

        return new BulkOperationResultDto(created, $"{created} session(s) generated.");
    }

    /// <summary>
    /// Expands the recurrence rule into concrete occurrence dates within the window.
    /// </summary>
    private static IEnumerable<DateOnly> ExpandOccurrences(
        RecurringSchedule schedule, DateOnly windowStart, DateOnly windowEnd)
    {
        var current = schedule.RangeStart > windowStart ? schedule.RangeStart : windowStart;
        var end     = schedule.RangeEnd.HasValue && schedule.RangeEnd < windowEnd
                        ? schedule.RangeEnd.Value
                        : windowEnd;

        return schedule.Frequency switch
        {
            "weekly"   => ExpandWeekly(current, end, schedule.ByWeekday, schedule.IntervalCount),
            "biweekly" => ExpandWeekly(current, end, schedule.ByWeekday, 2),
            "monthly"  => ExpandMonthly(current, end, schedule.ByMonthday),
            _          => []
        };
    }

    private static IEnumerable<DateOnly> ExpandWeekly(
        DateOnly start, DateOnly end, int[]? byWeekday, int intervalWeeks)
    {
        // If no specific weekday filter, emit one occurrence per interval starting from start
        if (byWeekday is null or { Length: 0 })
        {
            var d = start;
            while (d <= end)
            {
                yield return d;
                d = d.AddDays(7 * intervalWeeks);
            }
            yield break;
        }

        // Find the first week anchor
        var anchor = start;

        while (anchor <= end)
        {
            foreach (var dow in byWeekday.OrderBy(x => x))
            {
                // Find the date in this week matching the weekday
                var startOfWeek = anchor.AddDays(-(int)anchor.DayOfWeek);
                var candidate   = startOfWeek.AddDays(dow);

                if (candidate >= start && candidate <= end)
                    yield return candidate;
            }

            anchor = anchor.AddDays(7 * intervalWeeks);
        }
    }

    private static IEnumerable<DateOnly> ExpandMonthly(
        DateOnly start, DateOnly end, int[]? byMonthday)
    {
        if (byMonthday is null or { Length: 0 })
        {
            var d = start;
            while (d <= end)
            {
                yield return d;
                d = d.AddMonths(1);
            }
            yield break;
        }

        var current = new DateOnly(start.Year, start.Month, 1);

        while (current <= end)
        {
            foreach (var day in byMonthday.OrderBy(x => x))
            {
                var daysInMonth = DateTime.DaysInMonth(current.Year, current.Month);
                if (day < 1 || day > daysInMonth) continue;

                var candidate = new DateOnly(current.Year, current.Month, day);
                if (candidate >= start && candidate <= end)
                    yield return candidate;
            }

            current = current.AddMonths(1);
        }
    }
}

// ── BulkCancelSessionsCommand ─────────────────────────────────────────────────

/// <summary>
/// Cancels all upcoming scheduled sessions that belong to a recurring schedule.
/// Optionally restrict to a date range.
/// </summary>
public record BulkCancelSessionsCommand(
    Guid RecurringScheduleId,
    string? Reason,
    DateOnly? From,
    DateOnly? To) : IRequest<BulkOperationResultDto>;

public sealed class BulkCancelSessionsCommandHandler
    : IRequestHandler<BulkCancelSessionsCommand, BulkOperationResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BulkCancelSessionsCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BulkOperationResultDto> Handle(BulkCancelSessionsCommand req, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var q = _db.Sessions.Where(s =>
            s.RecurringScheduleId == req.RecurringScheduleId &&
            s.Status == "scheduled" &&
            s.StartsAt > now);

        if (req.From.HasValue)
        {
            var from = new DateTimeOffset(req.From.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(s => s.StartsAt >= from);
        }

        if (req.To.HasValue)
        {
            var to = new DateTimeOffset(req.To.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            q = q.Where(s => s.StartsAt <= to);
        }

        var sessions = await q.ToListAsync(ct);

        foreach (var s in sessions)
            s.Cancel(req.Reason, _currentUser.UserId);

        if (sessions.Count > 0)
            await _db.SaveChangesAsync(ct);

        return new BulkOperationResultDto(sessions.Count, $"{sessions.Count} session(s) cancelled.");
    }
}

// ── BulkReassignRoomCommand ───────────────────────────────────────────────────

/// <summary>
/// Reassigns the room for all future scheduled sessions of a recurring schedule from a given date.
/// Conflict-checks each session before moving it.
/// </summary>
public record BulkReassignRoomCommand(
    Guid RecurringScheduleId,
    Guid? NewRoomId,
    DateOnly From) : IRequest<BulkOperationResultDto>;

public sealed class BulkReassignRoomCommandHandler
    : IRequestHandler<BulkReassignRoomCommand, BulkOperationResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ISessionRepository _sessions;
    private readonly ICurrentUserService _currentUser;

    public BulkReassignRoomCommandHandler(
        IAppDbContext db, ISessionRepository sessions, ICurrentUserService currentUser)
    {
        _db = db;
        _sessions = sessions;
        _currentUser = currentUser;
    }

    public async Task<BulkOperationResultDto> Handle(BulkReassignRoomCommand req, CancellationToken ct)
    {
        var from = new DateTimeOffset(req.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var upcomingSessions = await _db.Sessions
            .Where(s => s.RecurringScheduleId == req.RecurringScheduleId
                     && s.Status == "scheduled"
                     && s.StartsAt >= from)
            .OrderBy(s => s.StartsAt)
            .ToListAsync(ct);

        int updated = 0;
        int skipped = 0;

        foreach (var s in upcomingSessions)
        {
            if (req.NewRoomId.HasValue)
            {
                var conflict = await _sessions.HasRoomConflictAsync(
                    req.NewRoomId.Value, s.StartsAt, s.EndsAt, s.Id, ct);
                if (conflict) { skipped++; continue; }
            }

            s.Reschedule(s.StartsAt, s.EndsAt, req.NewRoomId, _currentUser.UserId);
            updated++;
        }

        if (updated > 0)
            await _db.SaveChangesAsync(ct);

        return new BulkOperationResultDto(
            updated,
            $"{updated} session(s) reassigned; {skipped} skipped due to room conflict.");
    }
}
