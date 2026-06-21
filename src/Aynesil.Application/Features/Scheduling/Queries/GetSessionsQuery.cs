using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Queries;

// ── GetSessionsQuery ──────────────────────────────────────────────────────────

public class GetSessionsQuery : PagedQuery, IRequest<PaginatedResult<SessionListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? SessionTypeId { get; set; }
    public Guid? RecurringScheduleId { get; set; }
    public string? Status { get; set; }
    public bool? IsMakeup { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed class GetSessionsQueryHandler
    : IRequestHandler<GetSessionsQuery, PaginatedResult<SessionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetSessionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<SessionListItemDto>> Handle(
        GetSessionsQuery req, CancellationToken ct)
    {
        var q = _db.Sessions.AsNoTracking();

        if (req.CorporationId.HasValue)       q = q.Where(s => s.CorporationId == req.CorporationId.Value);
        if (req.CampusId.HasValue)            q = q.Where(s => s.CampusId == req.CampusId.Value);
        if (req.RoomId.HasValue)              q = q.Where(s => s.RoomId == req.RoomId.Value);
        if (req.SessionTypeId.HasValue)       q = q.Where(s => s.SessionTypeId == req.SessionTypeId.Value);
        if (req.RecurringScheduleId.HasValue) q = q.Where(s => s.RecurringScheduleId == req.RecurringScheduleId.Value);
        if (req.Status is not null)           q = q.Where(s => s.Status == req.Status);
        if (req.IsMakeup.HasValue)            q = q.Where(s => s.IsMakeup == req.IsMakeup.Value);
        if (req.From.HasValue)                q = q.Where(s => s.StartsAt >= req.From.Value);
        if (req.To.HasValue)                  q = q.Where(s => s.StartsAt <= req.To.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(x => x.Title != null && x.Title.ToLower().Contains(s));
        }

        var query =
            from s in q
            join r in _db.Rooms.AsNoTracking()
                on s.RoomId equals r.Id into roomGrp
            from r in roomGrp.DefaultIfEmpty()
            select new SessionListItemDto(
                s.Id, s.CorporationId, s.CampusId, s.SessionTypeId,
                s.RoomId, r != null ? r.Name : null,
                s.Title, s.StartsAt, s.EndsAt, s.Status, s.IsMakeup,
                s.Participants.Count(), s.Educators.Count());

        query = req.SortBy?.ToLower() switch
        {
            "startsat" => req.IsDescending ? query.OrderByDescending(s => s.StartsAt) : query.OrderBy(s => s.StartsAt),
            "status"   => req.IsDescending ? query.OrderByDescending(s => s.Status)   : query.OrderBy(s => s.Status),
            _          => query.OrderBy(s => s.StartsAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<SessionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetSessionQuery ───────────────────────────────────────────────────────────

public record GetSessionQuery(Guid Id) : IRequest<SessionDto>;

public sealed class GetSessionQueryHandler : IRequestHandler<GetSessionQuery, SessionDto>
{
    private readonly IAppDbContext _db;

    public GetSessionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SessionDto> Handle(GetSessionQuery req, CancellationToken ct)
        => await SchedulingProjection.LoadSessionAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Session {req.Id} not found.");
}
