using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Queries;

// ── CalendarViewDto ───────────────────────────────────────────────────────────

public record CalendarViewDto(
    IReadOnlyList<SessionListItemDto> Sessions,
    IReadOnlyList<CalendarEntryDto> Entries);

// ── GetSchoolCalendarQuery ────────────────────────────────────────────────────

public record GetSchoolCalendarQuery(
    Guid CorporationId,
    DateTimeOffset From,
    DateTimeOffset To) : IRequest<CalendarViewDto>;

public sealed class GetSchoolCalendarQueryHandler
    : IRequestHandler<GetSchoolCalendarQuery, CalendarViewDto>
{
    private readonly IAppDbContext _db;

    public GetSchoolCalendarQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CalendarViewDto> Handle(GetSchoolCalendarQuery req, CancellationToken ct)
    {
        var sessions = await CalendarHelpers.BuildSessionListQuery(_db, req.CorporationId, null, req.From, req.To)
            .OrderBy(s => s.StartsAt)
            .ToListAsync(ct);

        var rawEntries = await _db.CalendarEntries.AsNoTracking()
            .Where(e => e.CorporationId == req.CorporationId
                     && e.StartsAt < req.To
                     && e.EndsAt   > req.From)
            .ToListAsync(ct);

        var entries = rawEntries.Select(SchedulingProjection.ToCalendarEntryDto).ToList();
        return new CalendarViewDto(sessions, entries);
    }
}

// ── GetCampusCalendarQuery ────────────────────────────────────────────────────

public record GetCampusCalendarQuery(
    Guid CorporationId,
    Guid CampusId,
    DateTimeOffset From,
    DateTimeOffset To) : IRequest<CalendarViewDto>;

public sealed class GetCampusCalendarQueryHandler
    : IRequestHandler<GetCampusCalendarQuery, CalendarViewDto>
{
    private readonly IAppDbContext _db;

    public GetCampusCalendarQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CalendarViewDto> Handle(GetCampusCalendarQuery req, CancellationToken ct)
    {
        var sessions = await CalendarHelpers.BuildSessionListQuery(_db, req.CorporationId, req.CampusId, req.From, req.To)
            .OrderBy(s => s.StartsAt)
            .ToListAsync(ct);

        var rawEntries2 = await _db.CalendarEntries.AsNoTracking()
            .Where(e => e.CorporationId == req.CorporationId
                     && (e.CampusId == null || e.CampusId == req.CampusId)
                     && e.StartsAt < req.To
                     && e.EndsAt   > req.From)
            .ToListAsync(ct);
        var entries = rawEntries2.Select(SchedulingProjection.ToCalendarEntryDto).ToList();

        return new CalendarViewDto(sessions, entries);
    }
}

// ── GetRoomCalendarQuery ──────────────────────────────────────────────────────

public record GetRoomCalendarQuery(
    Guid RoomId,
    DateTimeOffset From,
    DateTimeOffset To) : IRequest<IReadOnlyList<SessionListItemDto>>;

public sealed class GetRoomCalendarQueryHandler
    : IRequestHandler<GetRoomCalendarQuery, IReadOnlyList<SessionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetRoomCalendarQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SessionListItemDto>> Handle(
        GetRoomCalendarQuery req, CancellationToken ct)
        => await _db.Sessions.AsNoTracking()
            .Where(s => s.RoomId == req.RoomId
                     && s.StartsAt < req.To
                     && s.EndsAt   > req.From)
            .Join(_db.Rooms.AsNoTracking(), s => s.RoomId, r => r.Id, (s, r) =>
                new SessionListItemDto(
                    s.Id, s.CorporationId, s.CampusId, s.SessionTypeId,
                    s.RoomId, r.Name, s.Title, s.StartsAt, s.EndsAt, s.Status, s.IsMakeup,
                    s.Participants.Count(), s.Educators.Count()))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(ct);
}

// ── GetEducatorCalendarQuery ──────────────────────────────────────────────────

public record GetEducatorCalendarQuery(
    Guid CorporationId,
    Guid EducatorId,
    DateTimeOffset From,
    DateTimeOffset To) : IRequest<IReadOnlyList<SessionListItemDto>>;

public sealed class GetEducatorCalendarQueryHandler
    : IRequestHandler<GetEducatorCalendarQuery, IReadOnlyList<SessionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetEducatorCalendarQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SessionListItemDto>> Handle(
        GetEducatorCalendarQuery req, CancellationToken ct)
    {
        var sessionIds = await _db.SessionEducators.AsNoTracking()
            .Where(se => se.EducatorId == req.EducatorId)
            .Select(se => se.SessionId)
            .ToListAsync(ct);

        return await _db.Sessions.AsNoTracking()
            .Where(s => s.CorporationId == req.CorporationId
                     && sessionIds.Contains(s.Id)
                     && s.StartsAt < req.To
                     && s.EndsAt   > req.From)
            .Select(s => new SessionListItemDto(
                s.Id, s.CorporationId, s.CampusId, s.SessionTypeId,
                s.RoomId, null, s.Title, s.StartsAt, s.EndsAt, s.Status, s.IsMakeup,
                s.Participants.Count(), s.Educators.Count()))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(ct);
    }
}

// ── GetStudentCalendarQuery ───────────────────────────────────────────────────

public record GetStudentCalendarQuery(
    Guid CorporationId,
    Guid StudentId,
    DateTimeOffset From,
    DateTimeOffset To) : IRequest<IReadOnlyList<SessionListItemDto>>;

public sealed class GetStudentCalendarQueryHandler
    : IRequestHandler<GetStudentCalendarQuery, IReadOnlyList<SessionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentCalendarQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SessionListItemDto>> Handle(
        GetStudentCalendarQuery req, CancellationToken ct)
    {
        var sessionIds = await _db.SessionParticipants.AsNoTracking()
            .Where(p => p.StudentId == req.StudentId)
            .Select(p => p.SessionId)
            .ToListAsync(ct);

        return await _db.Sessions.AsNoTracking()
            .Where(s => s.CorporationId == req.CorporationId
                     && sessionIds.Contains(s.Id)
                     && s.StartsAt < req.To
                     && s.EndsAt   > req.From)
            .Select(s => new SessionListItemDto(
                s.Id, s.CorporationId, s.CampusId, s.SessionTypeId,
                s.RoomId, null, s.Title, s.StartsAt, s.EndsAt, s.Status, s.IsMakeup,
                s.Participants.Count(), s.Educators.Count()))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(ct);
    }
}

// ── GetCalendarEntriesQuery ───────────────────────────────────────────────────

public record GetCalendarEntriesQuery(
    Guid CorporationId,
    DateTimeOffset From,
    DateTimeOffset To,
    Guid? CampusId) : IRequest<IReadOnlyList<CalendarEntryDto>>;

public sealed class GetCalendarEntriesQueryHandler
    : IRequestHandler<GetCalendarEntriesQuery, IReadOnlyList<CalendarEntryDto>>
{
    private readonly IAppDbContext _db;

    public GetCalendarEntriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CalendarEntryDto>> Handle(
        GetCalendarEntriesQuery req, CancellationToken ct)
    {
        var q = _db.CalendarEntries.AsNoTracking()
            .Where(e => e.CorporationId == req.CorporationId
                     && e.StartsAt < req.To
                     && e.EndsAt   > req.From);

        if (req.CampusId.HasValue)
            q = q.Where(e => e.CampusId == null || e.CampusId == req.CampusId.Value);

        var raw = await q.OrderBy(e => e.StartsAt).ToListAsync(ct);
        return raw.Select(SchedulingProjection.ToCalendarEntryDto).ToList();
    }
}

// ── Shared helper ─────────────────────────────────────────────────────────────

file static class CalendarHelpers
{
    public static IQueryable<SessionListItemDto> BuildSessionListQuery(
        IAppDbContext db,
        Guid corporationId,
        Guid? campusId,
        DateTimeOffset from,
        DateTimeOffset to)
    {
        var q = db.Sessions.AsNoTracking()
            .Where(s => s.CorporationId == corporationId
                     && s.StartsAt < to
                     && s.EndsAt   > from);

        if (campusId.HasValue)
            q = q.Where(s => s.CampusId == campusId.Value);

        return q.Select(s => new SessionListItemDto(
            s.Id, s.CorporationId, s.CampusId, s.SessionTypeId,
            s.RoomId, null, s.Title, s.StartsAt, s.EndsAt, s.Status, s.IsMakeup,
            s.Participants.Count(), s.Educators.Count()));
    }
}
