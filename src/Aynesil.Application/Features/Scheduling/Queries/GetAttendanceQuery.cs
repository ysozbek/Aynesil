using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Queries;

// ── GetSessionAttendanceQuery ─────────────────────────────────────────────────

public record GetSessionAttendanceQuery(Guid SessionId) : IRequest<IReadOnlyList<AttendanceDto>>;

public sealed class GetSessionAttendanceQueryHandler
    : IRequestHandler<GetSessionAttendanceQuery, IReadOnlyList<AttendanceDto>>
{
    private readonly IAppDbContext _db;

    public GetSessionAttendanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AttendanceDto>> Handle(
        GetSessionAttendanceQuery req, CancellationToken ct)
    {
        var records = await _db.Attendances.AsNoTracking()
            .Where(a => a.SessionId == req.SessionId)
            .ToListAsync(ct);

        var studentIds = records.Select(a => a.StudentId).ToList();
        var students = await _db.Students.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => new { s.Id, s.FirstName, s.LastName })
            .ToDictionaryAsync(s => s.Id, s => $"{s.FirstName} {s.LastName}".Trim(), ct);

        return records.Select(a => new AttendanceDto(
            a.Id, a.SessionId, a.StudentId,
            students.GetValueOrDefault(a.StudentId, ""),
            a.Status, a.ReasonId, a.MinutesAttended,
            a.Note, a.RecordedBy, a.RecordedAt)).ToList();
    }
}

// ── GetStudentAttendanceQuery ─────────────────────────────────────────────────

public class GetStudentAttendanceQuery : PagedQuery, IRequest<PaginatedResult<AttendanceDto>>
{
    public Guid CorporationId { get; set; }
    public Guid StudentId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? Status { get; set; }
}

public sealed class GetStudentAttendanceQueryHandler
    : IRequestHandler<GetStudentAttendanceQuery, PaginatedResult<AttendanceDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentAttendanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<AttendanceDto>> Handle(
        GetStudentAttendanceQuery req, CancellationToken ct)
    {
        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == req.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);
        var studentName = student is null ? "" : $"{student.FirstName} {student.LastName}".Trim();

        var q = _db.Attendances.AsNoTracking()
            .Where(a => a.CorporationId == req.CorporationId
                     && a.StudentId     == req.StudentId);

        if (req.From.HasValue)   q = q.Where(a => a.RecordedAt >= req.From.Value);
        if (req.To.HasValue)     q = q.Where(a => a.RecordedAt <= req.To.Value);
        if (req.Status is not null) q = q.Where(a => a.Status == req.Status);

        var query = q.Select(a => new AttendanceDto(
            a.Id, a.SessionId, a.StudentId, studentName,
            a.Status, a.ReasonId, a.MinutesAttended,
            a.Note, a.RecordedBy, a.RecordedAt));

        query = req.SortBy?.ToLower() switch
        {
            "status"     => req.IsDescending ? query.OrderByDescending(a => a.Status)     : query.OrderBy(a => a.Status),
            "recordedat" => req.IsDescending ? query.OrderByDescending(a => a.RecordedAt) : query.OrderBy(a => a.RecordedAt),
            _            => query.OrderByDescending(a => a.RecordedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<AttendanceDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetAttendanceSummaryQuery ─────────────────────────────────────────────────

public record GetAttendanceSummaryQuery(
    Guid CorporationId,
    Guid StudentId,
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<AttendanceSummaryDto>;

public sealed class GetAttendanceSummaryQueryHandler
    : IRequestHandler<GetAttendanceSummaryQuery, AttendanceSummaryDto>
{
    private readonly IAppDbContext _db;

    public GetAttendanceSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AttendanceSummaryDto> Handle(
        GetAttendanceSummaryQuery req, CancellationToken ct)
    {
        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == req.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var studentName = $"{student.FirstName} {student.LastName}".Trim();

        var q = _db.Attendances.AsNoTracking()
            .Where(a => a.CorporationId == req.CorporationId && a.StudentId == req.StudentId);

        if (req.From.HasValue) q = q.Where(a => a.RecordedAt >= req.From.Value);
        if (req.To.HasValue)   q = q.Where(a => a.RecordedAt <= req.To.Value);

        var counts = await q
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(string s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

        int present    = Get("present");
        int absent     = Get("absent");
        int late       = Get("late");
        int excused    = Get("excused");
        int leftEarly  = Get("left_early");
        int total      = present + absent + late + excused + leftEarly;

        decimal rate = total == 0 ? 0 :
            Math.Round((decimal)(present + late + leftEarly) / total * 100, 2);

        return new AttendanceSummaryDto(
            req.StudentId, studentName, total,
            present, absent, late, excused, leftEarly, rate);
    }
}
