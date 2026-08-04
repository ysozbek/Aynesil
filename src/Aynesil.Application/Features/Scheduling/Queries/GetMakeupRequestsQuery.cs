using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Queries;

// ── GetMakeupRequestsQuery ────────────────────────────────────────────────────

public class GetMakeupRequestsQuery : PagedQuery, IRequest<PaginatedResult<MakeupRequestListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? StudentId { get; set; }
    public string? Status { get; set; }
}

public sealed class GetMakeupRequestsQueryHandler
    : IRequestHandler<GetMakeupRequestsQuery, PaginatedResult<MakeupRequestListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetMakeupRequestsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<MakeupRequestListItemDto>> Handle(
        GetMakeupRequestsQuery req, CancellationToken ct)
    {
        var q = _db.MakeupRequests.AsNoTracking();

        if (req.CorporationId.HasValue) q = q.Where(m => m.CorporationId == req.CorporationId.Value);
        if (req.StudentId.HasValue)     q = q.Where(m => m.StudentId == req.StudentId.Value);
        if (req.Status is not null)     q = q.Where(m => m.Status == req.Status);

        var joined =
            from m in q
            join s in _db.Students.AsNoTracking()
                on m.StudentId equals s.Id
            select new { m, s };

        var sorted = req.SortBy?.ToLower() switch
        {
            "status"      => req.IsDescending ? joined.OrderByDescending(x => x.m.Status)      : joined.OrderBy(x => x.m.Status),
            "requestedat" => req.IsDescending ? joined.OrderByDescending(x => x.m.RequestedAt) : joined.OrderBy(x => x.m.RequestedAt),
            _             => joined.OrderByDescending(x => x.m.RequestedAt)
        };

        var total = await sorted.CountAsync(ct);
        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(x => new MakeupRequestListItemDto(
                x.m.Id, x.m.StudentId,
                x.s.FirstName + " " + x.s.LastName,
                x.m.MissedSessionId, x.m.Status,
                x.m.RequestedAt, x.m.ExpiresOn))
            .ToListAsync(ct);
        return PaginatedResult<MakeupRequestListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetMakeupRequestQuery ─────────────────────────────────────────────────────

public record GetMakeupRequestQuery(Guid Id) : IRequest<MakeupRequestDto>;

public sealed class GetMakeupRequestQueryHandler : IRequestHandler<GetMakeupRequestQuery, MakeupRequestDto>
{
    private readonly IAppDbContext _db;

    public GetMakeupRequestQueryHandler(IAppDbContext db) => _db = db;

    public async Task<MakeupRequestDto> Handle(GetMakeupRequestQuery req, CancellationToken ct)
    {
        var m = await _db.MakeupRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"MakeupRequest {req.Id} not found.");

        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == m.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);
        var name = student is null ? "" : $"{student.FirstName} {student.LastName}".Trim();

        return SchedulingProjection.ToMakeupDto(m, name);
    }
}
