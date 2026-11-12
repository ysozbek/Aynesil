using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Queries;

// ── GetRecurringSchedulesQuery ────────────────────────────────────────────────

public class GetRecurringSchedulesQuery
    : PagedQuery, IRequest<PaginatedResult<RecurringScheduleListItemDto>>
{
    public Guid? CampusId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetRecurringSchedulesQueryHandler
    : IRequestHandler<GetRecurringSchedulesQuery, PaginatedResult<RecurringScheduleListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetRecurringSchedulesQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResult<RecurringScheduleListItemDto>> Handle(
        GetRecurringSchedulesQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var q = _db.RecurringSchedules.AsNoTracking();

        q = q.Where(s => s.CorporationId == corporationId);
        if (req.CampusId.HasValue)      q = q.Where(s => s.CampusId == req.CampusId.Value);
        if (req.IsActive.HasValue)      q = q.Where(s => s.IsActive == req.IsActive.Value);

        var query = q.Select(s => new RecurringScheduleListItemDto(
            s.Id, s.CorporationId, s.CampusId, s.SessionTypeId,
            s.Frequency, s.StartTime, s.DurationMinutes,
            s.RangeStart, s.RangeEnd, s.IsActive,
            _db.Campuses.Where(c => c.Id == s.CampusId).Select(c => c.Name).FirstOrDefault(),
            s.RoomId,
            _db.Rooms.Where(r => r.Id == s.RoomId).Select(r => r.Name).FirstOrDefault(),
            s.StudentProgramId,
            _db.StudentPrograms.Where(sp => sp.Id == s.StudentProgramId)
                .Join(_db.Students, sp => sp.StudentId, st => st.Id, (sp, st) => st.FirstName + " " + st.LastName)
                .FirstOrDefault(),
            _db.StudentPrograms.Where(sp => sp.Id == s.StudentProgramId)
                .Join(_db.EducationPrograms, sp => sp.ProgramId, p => p.Id, (sp, p) => p.Name)
                .FirstOrDefault(),
            _db.ProgramServices.Where(ps => ps.Id == s.ProgramServiceId).Select(ps => ps.Name).FirstOrDefault(),
            s.Educators.Where(e => e.Role == "lead")
                .Join(_db.Educators, e => e.EducatorId, ed => ed.Id, (e, ed) => ed.FirstName + " " + ed.LastName)
                .FirstOrDefault(),
            _db.Sessions.Count(x => x.RecurringScheduleId == s.Id),
            s.ByWeekday));

        query = req.SortBy?.ToLower() switch
        {
            "rangestart" => req.IsDescending ? query.OrderByDescending(s => s.RangeStart) : query.OrderBy(s => s.RangeStart),
            "frequency"  => req.IsDescending ? query.OrderByDescending(s => s.Frequency)  : query.OrderBy(s => s.Frequency),
            _            => query.OrderByDescending(s => s.RangeStart)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<RecurringScheduleListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetRecurringScheduleQuery ─────────────────────────────────────────────────

public record GetRecurringScheduleQuery(Guid Id) : IRequest<RecurringScheduleDto>;

public sealed class GetRecurringScheduleQueryHandler
    : IRequestHandler<GetRecurringScheduleQuery, RecurringScheduleDto>
{
    private readonly IAppDbContext _db;

    public GetRecurringScheduleQueryHandler(IAppDbContext db) => _db = db;

    public async Task<RecurringScheduleDto> Handle(GetRecurringScheduleQuery req, CancellationToken ct)
    {
        var s = await _db.RecurringSchedules.AsNoTracking()
            .Include(x => x.Educators)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"RecurringSchedule {req.Id} not found.");

        return CreateRecurringScheduleCommandHandler.ToDto(s);
    }
}
