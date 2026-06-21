using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Programs.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Programs.Queries;

// ── GetStudentProgramsQuery ───────────────────────────────────────────────────

public class GetStudentProgramsQuery : PagedQuery, IRequest<PaginatedResult<StudentProgramListItemDto>>
{
    public Guid? StudentId { get; set; }
    public Guid? CorporationId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? CampusId { get; set; }
    public string? Status { get; set; }
    public Guid? EnrollmentId { get; set; }
}

public sealed class GetStudentProgramsQueryHandler
    : IRequestHandler<GetStudentProgramsQuery, PaginatedResult<StudentProgramListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentProgramsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<StudentProgramListItemDto>> Handle(
        GetStudentProgramsQuery req, CancellationToken ct)
    {
        var q = _db.StudentPrograms.AsNoTracking();

        if (req.StudentId.HasValue)
            q = q.Where(sp => sp.StudentId == req.StudentId.Value);
        if (req.CorporationId.HasValue)
            q = q.Where(sp => sp.CorporationId == req.CorporationId.Value);
        if (req.ProgramId.HasValue)
            q = q.Where(sp => sp.ProgramId == req.ProgramId.Value);
        if (req.CampusId.HasValue)
            q = q.Where(sp => sp.CampusId == req.CampusId.Value);
        if (req.Status is not null)
            q = q.Where(sp => sp.Status == req.Status);
        if (req.EnrollmentId.HasValue)
            q = q.Where(sp => sp.EnrollmentId == req.EnrollmentId.Value);

        var query =
            from sp in q
            join prog in _db.EducationPrograms.AsNoTracking() on sp.ProgramId equals prog.Id
            join pt in _db.RefValues.AsNoTracking()
                on prog.ProgramTypeId equals pt.Id into ptGrp
            from pt in ptGrp.DefaultIfEmpty()
            join campus in _db.Campuses.AsNoTracking()
                on sp.CampusId equals campus.Id into campusGrp
            from campus in campusGrp.DefaultIfEmpty()
            select new StudentProgramListItemDto(
                sp.Id, sp.StudentId,
                sp.ProgramId, prog.Name, prog.Code,
                pt != null ? pt.Code : null,
                sp.CampusId, campus != null ? campus.Name : null,
                sp.StartDate, sp.Status,
                sp.CreatedAt);

        query = req.SortBy?.ToLower() switch
        {
            "programname" => req.IsDescending ? query.OrderByDescending(sp => sp.ProgramName) : query.OrderBy(sp => sp.ProgramName),
            "startdate"   => req.IsDescending ? query.OrderByDescending(sp => sp.StartDate)   : query.OrderBy(sp => sp.StartDate),
            "createdat"   => req.IsDescending ? query.OrderByDescending(sp => sp.CreatedAt)   : query.OrderBy(sp => sp.CreatedAt),
            _             => req.IsDescending ? query.OrderByDescending(sp => sp.CreatedAt)   : query.OrderBy(sp => sp.ProgramName)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        return PaginatedResult<StudentProgramListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetStudentProgramQuery ────────────────────────────────────────────────────

public record GetStudentProgramQuery(Guid Id) : IRequest<StudentProgramDto>;

public sealed class GetStudentProgramQueryHandler : IRequestHandler<GetStudentProgramQuery, StudentProgramDto>
{
    private readonly IAppDbContext _db;

    public GetStudentProgramQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StudentProgramDto> Handle(GetStudentProgramQuery req, CancellationToken ct)
    {
        var sp = await _db.StudentPrograms.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"StudentProgram {req.Id} not found.");

        var program = await _db.EducationPrograms.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == sp.ProgramId, ct);
        var campusName = sp.CampusId.HasValue
            ? await _db.Campuses.AsNoTracking()
                .Where(c => c.Id == sp.CampusId.Value)
                .Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;

        return ProgramProjection.ToStudentProgramDto(sp, program?.Name, program?.Code, campusName);
    }
}
