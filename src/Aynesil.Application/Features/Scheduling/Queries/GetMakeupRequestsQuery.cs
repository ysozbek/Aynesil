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

        var query =
            from m in q
            join s in _db.Students.AsNoTracking()
                on m.StudentId equals s.Id
            select new MakeupRequestListItemDto(
                m.Id, m.StudentId,
                s.FirstName + " " + s.LastName,
                m.MissedSessionId, m.Status,
                m.RequestedAt, m.ExpiresOn);

        query = req.SortBy?.ToLower() switch
        {
            "status"      => req.IsDescending ? query.OrderByDescending(m => m.Status) : query.OrderBy(m => m.Status),
            "requestedat" => req.IsDescending ? query.OrderByDescending(m => m.RequestedAt) : query.OrderBy(m => m.RequestedAt),
            _             => query.OrderByDescending(m => m.RequestedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
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
