using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Meetings.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Meetings.Queries;

// ── GetMeetingsQuery ──────────────────────────────────────────────────────────

/// <summary>
/// Paginated list of meetings. Supports filtering by corporation, campus, type,
/// status, organizer, and scheduled date range. Used for school-level admin views.
/// </summary>
public class GetMeetingsQuery : PagedQuery, IRequest<PaginatedResult<MeetingListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? MeetingTypeId { get; set; }
    public string? Status { get; set; }
    public Guid? OrganizerId { get; set; }
    public DateTimeOffset? ScheduledFrom { get; set; }
    public DateTimeOffset? ScheduledTo { get; set; }
}

public sealed class GetMeetingsQueryHandler
    : IRequestHandler<GetMeetingsQuery, PaginatedResult<MeetingListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetMeetingsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<MeetingListItemDto>> Handle(
        GetMeetingsQuery req, CancellationToken ct)
    {
        var q = _db.Meetings.AsNoTracking()
            .Where(m => m.DeletedAt == null);

        if (req.CorporationId.HasValue)
            q = q.Where(m => m.CorporationId == req.CorporationId.Value);

        if (req.CampusId.HasValue)
            q = q.Where(m => m.CampusId == req.CampusId.Value);

        if (req.MeetingTypeId.HasValue)
            q = q.Where(m => m.MeetingTypeId == req.MeetingTypeId.Value);

        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(m => m.Status == req.Status);

        if (req.OrganizerId.HasValue)
            q = q.Where(m => m.OrganizerId == req.OrganizerId.Value);

        if (req.ScheduledFrom.HasValue)
            q = q.Where(m => m.ScheduledAt >= req.ScheduledFrom.Value);

        if (req.ScheduledTo.HasValue)
            q = q.Where(m => m.ScheduledAt <= req.ScheduledTo.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(m => m.Title.ToLower().Contains(term)
                           || (m.Location != null && m.Location.ToLower().Contains(term)));
        }

        // Sort on entity/join before DTO projection — EF cannot translate OrderBy on DTO ctor.
        var joined =
            from m in q
            join typ in _db.RefValues.AsNoTracking()
                on m.MeetingTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new { m, typ };

        var sorted = req.SortBy?.ToLowerInvariant() switch
        {
            "title"       => req.IsDescending ? joined.OrderByDescending(x => x.m.Title)       : joined.OrderBy(x => x.m.Title),
            "scheduledat" => req.IsDescending ? joined.OrderByDescending(x => x.m.ScheduledAt) : joined.OrderBy(x => x.m.ScheduledAt),
            "status"      => req.IsDescending ? joined.OrderByDescending(x => x.m.Status)      : joined.OrderBy(x => x.m.Status),
            _             => joined.OrderByDescending(x => x.m.ScheduledAt)
        };

        var total = await sorted.CountAsync(ct);
        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(x => new MeetingListItemDto(
                x.m.Id,
                x.m.CorporationId,
                x.m.CampusId,
                x.m.MeetingTypeId,
                x.typ != null ? x.typ.Code : null,
                x.m.Title,
                x.m.Location,
                x.m.ScheduledAt,
                x.m.EndsAt,
                x.m.Status,
                x.m.OrganizerId,
                x.m.Participants.Count,
                x.m.UpdatedAt))
            .ToListAsync(ct);
        return PaginatedResult<MeetingListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetMeetingQuery ───────────────────────────────────────────────────────────

/// <summary>
/// Full meeting detail including all participants, outcomes, and follow-ups.
/// </summary>
public record GetMeetingQuery(Guid Id) : IRequest<MeetingDto>;

public sealed class GetMeetingQueryHandler : IRequestHandler<GetMeetingQuery, MeetingDto>
{
    private readonly IAppDbContext _db;

    public GetMeetingQueryHandler(IAppDbContext db) => _db = db;

    public async Task<MeetingDto> Handle(GetMeetingQuery req, CancellationToken ct)
    {
        var meeting = await _db.Meetings.AsNoTracking()
            .Include(m => m.Participants)
            .Include(m => m.Outcomes)
            .Include(m => m.FollowUps)
            .FirstOrDefaultAsync(m => m.Id == req.Id && m.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Meeting {req.Id} not found.");

        var typeCode = meeting.MeetingTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == meeting.MeetingTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return new MeetingDto(
            meeting.Id, meeting.CorporationId, meeting.CampusId,
            meeting.MeetingTypeId, typeCode,
            meeting.Title, meeting.Location, meeting.RoomId,
            meeting.ScheduledAt, meeting.EndsAt, meeting.Status,
            meeting.OrganizerId,
            meeting.CreatedAt, meeting.CreatedBy, meeting.UpdatedAt, meeting.RowVersion,
            meeting.Participants
                .Select(p => new MeetingParticipantDto(
                    p.Id, p.MeetingId, p.CorporationId,
                    p.ParticipantType, p.UserId, p.GuardianId,
                    p.LeadId, p.ExternalName, p.Attendance))
                .ToList(),
            meeting.Outcomes
                .OrderBy(o => o.CreatedAt)
                .Select(o => new MeetingOutcomeDto(
                    o.Id, o.MeetingId, o.Summary, o.Decisions,
                    o.CreatedAt, o.CreatedBy))
                .ToList(),
            meeting.FollowUps
                .OrderBy(f => f.CreatedAt)
                .Select(f => new MeetingFollowUpDto(
                    f.Id, f.MeetingId, f.Action,
                    f.AssigneeId, f.DueDate, f.Status, f.CreatedAt))
                .ToList());
    }
}

// ── GetMeetingCalendarQuery ───────────────────────────────────────────────────

/// <summary>
/// Returns meetings as calendar items within a date range.
/// Supports all four calendar contexts:
///   - School Calendar:    filter by CorporationId only
///   - Campus Calendar:    filter by CorporationId + CampusId
///   - Educator Calendar:  filter by OrganizerId OR UserId (participant lookup)
///   - Student/Parent Calendar: filter by GuardianId (participant lookup)
/// </summary>
public class GetMeetingCalendarQuery : IRequest<IReadOnlyList<MeetingCalendarItemDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? OrganizerId { get; set; }

    /// <summary>Filter to meetings where this user is a participant (educator calendar).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Filter to meetings where this guardian is a participant (parent/student calendar).</summary>
    public Guid? GuardianId { get; set; }

    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
}

public sealed class GetMeetingCalendarQueryHandler
    : IRequestHandler<GetMeetingCalendarQuery, IReadOnlyList<MeetingCalendarItemDto>>
{
    private readonly IAppDbContext _db;

    public GetMeetingCalendarQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MeetingCalendarItemDto>> Handle(
        GetMeetingCalendarQuery req, CancellationToken ct)
    {
        var q = _db.Meetings.AsNoTracking()
            .Where(m => m.DeletedAt == null
                     && m.CorporationId == req.CorporationId
                     && m.Status != "cancelled");

        // Date range: meetings that overlap with the requested window
        if (req.From != default)
            q = q.Where(m => m.EndsAt == null
                ? m.ScheduledAt >= req.From
                : m.EndsAt >= req.From);

        if (req.To != default)
            q = q.Where(m => m.ScheduledAt == null || m.ScheduledAt <= req.To);

        if (req.CampusId.HasValue)
            q = q.Where(m => m.CampusId == req.CampusId.Value);

        if (req.OrganizerId.HasValue)
            q = q.Where(m => m.OrganizerId == req.OrganizerId.Value);

        // Educator calendar: meetings where the user is a participant
        if (req.UserId.HasValue)
        {
            var meetingIdsWithUser = _db.MeetingParticipants.AsNoTracking()
                .Where(p => p.UserId == req.UserId.Value)
                .Select(p => p.MeetingId);

            q = q.Where(m => m.OrganizerId == req.UserId.Value
                           || meetingIdsWithUser.Contains(m.Id));
        }

        // Parent/student calendar: meetings where the guardian is a participant
        if (req.GuardianId.HasValue)
        {
            var meetingIdsWithGuardian = _db.MeetingParticipants.AsNoTracking()
                .Where(p => p.GuardianId == req.GuardianId.Value)
                .Select(p => p.MeetingId);

            q = q.Where(m => meetingIdsWithGuardian.Contains(m.Id));
        }

        var results = await (
            from m in q
            join typ in _db.RefValues.AsNoTracking()
                on m.MeetingTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            orderby m.ScheduledAt
            select new MeetingCalendarItemDto(
                m.Id, m.Title,
                m.ScheduledAt, m.EndsAt,
                m.MeetingTypeId,
                typ != null ? typ.Code : null,
                m.Status, m.CampusId, m.Location,
                m.OrganizerId,
                m.Participants.Count)
        ).ToListAsync(ct);

        return results;
    }
}
