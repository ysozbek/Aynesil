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

        var query =
            from sch in q
            join student in _db.Students.AsNoTracking()
                on sch.StudentId equals student.Id
            select new ScholarshipListItemDto(
                sch.Id, sch.StudentId,
                student.FirstName + " " + student.LastName,
                sch.ScholarshipTypeId,
                sch.Percentage, sch.Amount,
                sch.ValidFrom, sch.ValidTo);

        query = req.SortBy?.ToLower() switch
        {
            "studentname" => req.IsDescending ? query.OrderByDescending(s => s.StudentFullName) : query.OrderBy(s => s.StudentFullName),
            "validfrom"   => req.IsDescending ? query.OrderByDescending(s => s.ValidFrom)       : query.OrderBy(s => s.ValidFrom),
            _             => query.OrderBy(s => s.StudentFullName)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

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
