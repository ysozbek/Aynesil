using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Queries;

// ── GetSessionsQuery ──────────────────────────────────────────────────────────

public class GetSessionsQuery : PagedQuery, IRequest<PaginatedResult<SessionListItemDto>>
{
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
    private readonly ITenantContext _tenant;

    public GetSessionsQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResult<SessionListItemDto>> Handle(
        GetSessionsQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var q = _db.Sessions.AsNoTracking();

        q = q.Where(s => s.CorporationId == corporationId);
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

        var baseQ =
            from s in q
            join r in _db.Rooms.AsNoTracking()
                on s.RoomId equals r.Id into roomGrp
            from r in roomGrp.DefaultIfEmpty()
            select new { s, r };

        var sortedQ = req.SortBy?.ToLower() switch
        {
            "startsat" => req.IsDescending ? baseQ.OrderByDescending(x => x.s.StartsAt) : baseQ.OrderBy(x => x.s.StartsAt),
            "status"   => req.IsDescending ? baseQ.OrderByDescending(x => x.s.Status)   : baseQ.OrderBy(x => x.s.Status),
            _          => baseQ.OrderByDescending(x => x.s.StartsAt)
        };

        var total = await sortedQ.CountAsync(ct);
        var items = await sortedQ
            .Skip(req.Skip).Take(req.PageSize)
            .Select(x => new SessionListItemDto(
                x.s.Id, x.s.CorporationId, x.s.CampusId, x.s.SessionTypeId,
                x.s.RoomId, x.r != null ? x.r.Name : null,
                x.s.Title, x.s.StartsAt, x.s.EndsAt, x.s.Status, x.s.IsMakeup,
                x.s.Participants.Count(), x.s.Educators.Count(),
                _db.Campuses.Where(c => c.Id == x.s.CampusId).Select(c => c.Name).FirstOrDefault(),
                null))
            .ToListAsync(ct);
        items = await SchedulingProjection.WithSessionTypeNamesAsync(_db, items, _tenant.Locale, ct);
        return PaginatedResult<SessionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetSessionQuery ───────────────────────────────────────────────────────────

public record GetSessionQuery(Guid Id) : IRequest<SessionDto>;

public sealed class GetSessionQueryHandler : IRequestHandler<GetSessionQuery, SessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetSessionQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<SessionDto> Handle(GetSessionQuery req, CancellationToken ct)
        => await SchedulingProjection.LoadSessionAsync(_db, req.Id, _tenant.Locale, ct)
           ?? throw new KeyNotFoundException($"Session {req.Id} not found.");
}
