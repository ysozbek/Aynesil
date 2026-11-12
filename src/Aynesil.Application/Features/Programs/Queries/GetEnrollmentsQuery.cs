using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Programs.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Programs.Queries;

// ── GetStudentEnrollmentsQuery ────────────────────────────────────────────────

public class GetStudentEnrollmentsQuery : PagedQuery, IRequest<PaginatedResult<EnrollmentListItemDto>>
{
    public Guid? StudentId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? StatusId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetStudentEnrollmentsQueryHandler
    : IRequestHandler<GetStudentEnrollmentsQuery, PaginatedResult<EnrollmentListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetStudentEnrollmentsQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResult<EnrollmentListItemDto>> Handle(
        GetStudentEnrollmentsQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var q = _db.Enrollments.AsNoTracking();

        if (req.StudentId.HasValue)
            q = q.Where(e => e.StudentId == req.StudentId.Value);
        q = q.Where(e => e.CorporationId == corporationId);
        if (req.CampusId.HasValue)
            q = q.Where(e => e.CampusId == req.CampusId.Value);
        if (req.StatusId.HasValue)
            q = q.Where(e => e.StatusId == req.StatusId.Value);
        if (req.IsActive.HasValue)
            q = req.IsActive.Value
                ? q.Where(e => e.EndedOn == null)
                : q.Where(e => e.EndedOn != null);

        var query =
            from e in q
            join student in _db.Students.AsNoTracking() on e.StudentId equals student.Id
            join campus in _db.Campuses.AsNoTracking()
                on e.CampusId equals campus.Id into campusGrp
            from campus in campusGrp.DefaultIfEmpty()
            join status in _db.RefValues.AsNoTracking()
                on e.StatusId equals status.Id into statusGrp
            from status in statusGrp.DefaultIfEmpty()
            select new
            {
                e.Id,
                e.StudentId,
                StudentFullName = student.FirstName + " " + student.LastName,
                e.CampusId,
                CampusName = campus != null ? campus.Name : null,
                e.StatusId,
                StatusLabel = status != null ? status.Code : null,
                e.EnrolledOn,
                e.EndedOn,
                ProgramCount = e.StudentPrograms.Count(sp => sp.DeletedAt == null)
            };

        query = req.SortBy?.ToLower() switch
        {
            "enrolledon" => req.IsDescending ? query.OrderByDescending(e => e.EnrolledOn) : query.OrderBy(e => e.EnrolledOn),
            "student"    => req.IsDescending ? query.OrderByDescending(e => e.StudentFullName) : query.OrderBy(e => e.StudentFullName),
            _            => req.IsDescending ? query.OrderByDescending(e => e.EnrolledOn) : query.OrderByDescending(e => e.EnrolledOn)
        };

        var projected = query.Select(r =>
                new EnrollmentListItemDto(
                    r.Id,
                    r.StudentId,
                    r.StudentFullName,
                    r.CampusId,
                    r.CampusName,
                    r.StatusId,
                    r.StatusLabel,
                    r.EnrolledOn,
                    r.EndedOn,
                    r.ProgramCount));

        var total = await projected.CountAsync(ct);
        var items = await projected.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        return PaginatedResult<EnrollmentListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetEnrollmentQuery ────────────────────────────────────────────────────────

public record GetEnrollmentQuery(Guid Id) : IRequest<EnrollmentDto>;

public sealed class GetEnrollmentQueryHandler : IRequestHandler<GetEnrollmentQuery, EnrollmentDto>
{
    private readonly IAppDbContext _db;

    public GetEnrollmentQueryHandler(IAppDbContext db) => _db = db;

    public async Task<EnrollmentDto> Handle(GetEnrollmentQuery req, CancellationToken ct)
        => await ProgramProjection.LoadEnrollmentAsync(_db, req.Id, ct)
           ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Enrollment"));
}
