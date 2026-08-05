using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetScholarshipsQuery ──────────────────────────────────────────────────────

public class GetScholarshipsQuery : PagedQuery, IRequest<PaginatedResult<ScholarshipListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? ScholarshipTypeId { get; set; }
    public bool? ActiveOn { get; set; }
}

public sealed class GetScholarshipsQueryHandler
    : IRequestHandler<GetScholarshipsQuery, PaginatedResult<ScholarshipListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetScholarshipsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ScholarshipListItemDto>> Handle(
        GetScholarshipsQuery req, CancellationToken ct)
    {
        var q = _db.Scholarships.AsNoTracking();

        if (req.CorporationId.HasValue)   q = q.Where(s => s.CorporationId == req.CorporationId.Value);
        if (req.StudentId.HasValue)        q = q.Where(s => s.StudentId == req.StudentId.Value);
        if (req.ScholarshipTypeId.HasValue) q = q.Where(s => s.ScholarshipTypeId == req.ScholarshipTypeId.Value);

        if (req.ActiveOn == true)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            q = q.Where(s =>
                (s.ValidFrom == null || s.ValidFrom <= today) &&
                (s.ValidTo   == null || s.ValidTo   >= today));
        }

        // Sort on entity/join before DTO projection — EF cannot translate OrderBy on DTO ctor.
        var joined =
            from sch in q
            join student in _db.Students.AsNoTracking()
                on sch.StudentId equals student.Id
            select new { sch, student };

        var sorted = req.SortBy?.ToLower() switch
        {
            "studentname" => req.IsDescending
                ? joined.OrderByDescending(x => x.student.LastName).ThenByDescending(x => x.student.FirstName)
                : joined.OrderBy(x => x.student.LastName).ThenBy(x => x.student.FirstName),
            "validfrom" => req.IsDescending
                ? joined.OrderByDescending(x => x.sch.ValidFrom)
                : joined.OrderBy(x => x.sch.ValidFrom),
            _ => joined.OrderBy(x => x.student.LastName).ThenBy(x => x.student.FirstName)
        };

        var total = await sorted.CountAsync(ct);
        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(x => new ScholarshipListItemDto(
                x.sch.Id, x.sch.StudentId,
                x.student.FirstName + " " + x.student.LastName,
                x.sch.ScholarshipTypeId,
                x.sch.Percentage, x.sch.Amount,
                x.sch.ValidFrom, x.sch.ValidTo))
            .ToListAsync(ct);

        return PaginatedResult<ScholarshipListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetScholarshipQuery ───────────────────────────────────────────────────────

public record GetScholarshipQuery(Guid Id) : IRequest<ScholarshipDto>;

public sealed class GetScholarshipQueryHandler : IRequestHandler<GetScholarshipQuery, ScholarshipDto>
{
    private readonly IAppDbContext _db;

    public GetScholarshipQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ScholarshipDto> Handle(GetScholarshipQuery req, CancellationToken ct)
    {
        var sch = await _db.Scholarships.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Scholarship {req.Id} not found.");

        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == sch.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);

        var studentName = student is null ? "" : $"{student.FirstName} {student.LastName}".Trim();

        return new ScholarshipDto(
            sch.Id, sch.CorporationId, sch.StudentId, studentName,
            sch.ScholarshipTypeId, sch.Percentage, sch.Amount,
            sch.ValidFrom, sch.ValidTo, sch.ApprovedBy, sch.Note,
            sch.CreatedAt, sch.UpdatedAt, sch.RowVersion);
    }
}
