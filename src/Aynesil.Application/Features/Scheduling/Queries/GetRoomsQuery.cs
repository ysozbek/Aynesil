using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Queries;

// ── GetRoomsQuery ─────────────────────────────────────────────────────────────

public class GetRoomsQuery : PagedQuery, IRequest<PaginatedResult<RoomListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public bool? IsVirtual { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetRoomsQueryHandler
    : IRequestHandler<GetRoomsQuery, PaginatedResult<RoomListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetRoomsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<RoomListItemDto>> Handle(
        GetRoomsQuery req, CancellationToken ct)
    {
        var q = _db.Rooms.AsNoTracking();

        if (req.CorporationId.HasValue) q = q.Where(r => r.CorporationId == req.CorporationId.Value);
        if (req.CampusId.HasValue)      q = q.Where(r => r.CampusId == req.CampusId.Value);
        if (req.IsVirtual.HasValue)     q = q.Where(r => r.IsVirtual == req.IsVirtual.Value);
        if (req.IsActive.HasValue)      q = q.Where(r => r.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(r => r.Name.ToLower().Contains(s) || r.Code.ToLower().Contains(s));
        }

        var query = q.Select(r => new RoomListItemDto(
            r.Id, r.CorporationId, r.CampusId,
            r.Code, r.Name, r.Capacity, r.IsVirtual, r.IsActive));

        query = req.SortBy?.ToLower() switch
        {
            "name"     => req.IsDescending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
            "code"     => req.IsDescending ? query.OrderByDescending(r => r.Code) : query.OrderBy(r => r.Code),
            "capacity" => req.IsDescending ? query.OrderByDescending(r => r.Capacity) : query.OrderBy(r => r.Capacity),
            _          => query.OrderBy(r => r.Name)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<RoomListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetRoomQuery ──────────────────────────────────────────────────────────────

public record GetRoomQuery(Guid Id) : IRequest<RoomDto>;

public sealed class GetRoomQueryHandler : IRequestHandler<GetRoomQuery, RoomDto>
{
    private readonly IAppDbContext _db;

    public GetRoomQueryHandler(IAppDbContext db) => _db = db;

    public async Task<RoomDto> Handle(GetRoomQuery req, CancellationToken ct)
    {
        var room = await _db.Rooms.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Room {req.Id} not found.");

        return SchedulingProjection.ToRoomDto(room);
    }
}
