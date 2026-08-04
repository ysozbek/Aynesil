using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Queries;

// ── GetStudentsQuery ──────────────────────────────────────────────────────────

public class GetStudentsQuery : PagedQuery, IRequest<PaginatedResult<StudentListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? StatusId { get; set; }
    public bool? HasLead { get; set; }
    public DateOnly? BirthDateFrom { get; set; }
    public DateOnly? BirthDateTo { get; set; }
}

public sealed class GetStudentsQueryHandler
    : IRequestHandler<GetStudentsQuery, PaginatedResult<StudentListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<StudentListItemDto>> Handle(
        GetStudentsQuery req, CancellationToken ct)
    {
        var baseQuery = _db.Students.AsNoTracking();

        if (req.CorporationId.HasValue)
            baseQuery = baseQuery.Where(s => s.CorporationId == req.CorporationId.Value);

        if (req.CampusId.HasValue)
            baseQuery = baseQuery.Where(s => s.PrimaryCampusId == req.CampusId.Value);

        if (req.StatusId.HasValue)
            baseQuery = baseQuery.Where(s => s.StatusId == req.StatusId.Value);

        if (req.HasLead.HasValue)
            baseQuery = req.HasLead.Value
                ? baseQuery.Where(s => s.LeadId != null)
                : baseQuery.Where(s => s.LeadId == null);

        if (req.BirthDateFrom.HasValue)
            baseQuery = baseQuery.Where(s => s.BirthDate >= req.BirthDateFrom.Value);

        if (req.BirthDateTo.HasValue)
            baseQuery = baseQuery.Where(s => s.BirthDate <= req.BirthDateTo.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(s =>
                (s.FirstName + " " + s.LastName).ToLower().Contains(search) ||
                (s.StudentNo != null && s.StudentNo.ToLower().Contains(search)));
        }

        var baseQ =
            from s in baseQuery
            join campus in _db.Campuses.AsNoTracking()
                on s.PrimaryCampusId equals campus.Id into campusGrp
            from campus in campusGrp.DefaultIfEmpty()
            join status in _db.RefValues.AsNoTracking()
                on s.StatusId equals status.Id into statusGrp
            from status in statusGrp.DefaultIfEmpty()
            select new { s, campus, status };

        var sortedQ = req.SortBy?.ToLower() switch
        {
            "lastname"  => req.IsDescending ? baseQ.OrderByDescending(x => x.s.LastName)  : baseQ.OrderBy(x => x.s.LastName),
            "createdat" => req.IsDescending ? baseQ.OrderByDescending(x => x.s.CreatedAt) : baseQ.OrderBy(x => x.s.CreatedAt),
            "birthdate" => req.IsDescending ? baseQ.OrderByDescending(x => x.s.BirthDate) : baseQ.OrderBy(x => x.s.BirthDate),
            _           => req.IsDescending ? baseQ.OrderByDescending(x => x.s.CreatedAt) : baseQ.OrderBy(x => x.s.LastName)
        };

        var total = await sortedQ.CountAsync(ct);
        var items = await sortedQ
            .Skip(req.Skip).Take(req.PageSize)
            .Select(x => new StudentListItemDto(
                x.s.Id, x.s.StudentNo, x.s.FirstName, x.s.LastName,
                x.s.FirstName + " " + x.s.LastName,
                x.s.BirthDate, x.s.Gender,
                x.s.PrimaryCampusId, x.campus != null ? x.campus.Name : null,
                x.s.StatusId, x.status != null ? x.status.Code : null,
                x.s.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<StudentListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetStudentQuery ───────────────────────────────────────────────────────────

public record GetStudentQuery(Guid Id) : IRequest<StudentDto>;

public sealed class GetStudentQueryHandler : IRequestHandler<GetStudentQuery, StudentDto>
{
    private readonly IAppDbContext _db;

    public GetStudentQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StudentDto> Handle(GetStudentQuery req, CancellationToken ct)
        => await StudentProjection.LoadStudentAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Student {req.Id} not found.");
}

// ── GetStudentSummaryQuery ────────────────────────────────────────────────────

public record GetStudentSummaryQuery(Guid Id) : IRequest<StudentSummaryDto>;

public sealed class GetStudentSummaryQueryHandler
    : IRequestHandler<GetStudentSummaryQuery, StudentSummaryDto>
{
    private readonly IAppDbContext _db;

    public GetStudentSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StudentSummaryDto> Handle(GetStudentSummaryQuery req, CancellationToken ct)
    {
        var dto = await (
            from s in _db.Students.AsNoTracking()
            join campus in _db.Campuses.AsNoTracking()
                on s.PrimaryCampusId equals campus.Id into campusGrp
            from campus in campusGrp.DefaultIfEmpty()
            join status in _db.RefValues.AsNoTracking()
                on s.StatusId equals status.Id into statusGrp
            from status in statusGrp.DefaultIfEmpty()
            where s.Id == req.Id
            select new StudentSummaryDto(
                s.Id, s.StudentNo, s.FirstName, s.LastName,
                s.FirstName + " " + s.LastName,
                s.BirthDate, s.PhotoFileId,
                s.StatusId, status != null ? status.Code : null,
                campus != null ? campus.Name : null)
        ).FirstOrDefaultAsync(ct)
         ?? throw new KeyNotFoundException($"Student {req.Id} not found.");

        return dto;
    }
}

