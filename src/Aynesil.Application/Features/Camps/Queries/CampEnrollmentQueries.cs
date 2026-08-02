using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Queries;

// ── GetCampEnrollmentsQuery ───────────────────────────────────────────────────

public class GetCampEnrollmentsQuery : PagedQuery, IRequest<PaginatedResult<CampEnrollmentListItemDto>>
{
    public Guid? CampPeriodId { get; set; }
    public Guid? StudentId { get; set; }
    public string? Status { get; set; }
}

public sealed class GetCampEnrollmentsQueryHandler
    : IRequestHandler<GetCampEnrollmentsQuery, PaginatedResult<CampEnrollmentListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetCampEnrollmentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<CampEnrollmentListItemDto>> Handle(
        GetCampEnrollmentsQuery req, CancellationToken ct)
    {
        var q = _db.CampEnrollments.AsNoTracking().AsQueryable();

        if (req.CampPeriodId.HasValue)
            q = q.Where(e => e.CampPeriodId == req.CampPeriodId.Value);

        if (req.StudentId.HasValue)
            q = q.Where(e => e.StudentId == req.StudentId.Value);

        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(e => e.Status == req.Status);

        var query = q.Select(e => new CampEnrollmentListItemDto(
            e.Id,
            e.CampPeriodId,
            e.StudentId,
            e.StudentPackageId,
            e.Status,
            e.EnrolledAt));

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "status"     => req.IsDescending ? query.OrderByDescending(x => x.Status)     : query.OrderBy(x => x.Status),
            "enrolledat" => req.IsDescending ? query.OrderByDescending(x => x.EnrolledAt) : query.OrderBy(x => x.EnrolledAt),
            _            => query.OrderByDescending(x => x.EnrolledAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<CampEnrollmentListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetCampEnrollmentQuery ────────────────────────────────────────────────────

public record GetCampEnrollmentQuery(Guid Id) : IRequest<CampEnrollmentDto>;

public sealed class GetCampEnrollmentQueryHandler
    : IRequestHandler<GetCampEnrollmentQuery, CampEnrollmentDto>
{
    private readonly IAppDbContext _db;

    public GetCampEnrollmentQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CampEnrollmentDto> Handle(GetCampEnrollmentQuery req, CancellationToken ct)
    {
        var enrollment = await _db.CampEnrollments.AsNoTracking()
            .Include(e => e.Attendances)
            .FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Enrollment {req.Id} not found.");

        return new CampEnrollmentDto(
            enrollment.Id,
            enrollment.CorporationId,
            enrollment.CampPeriodId,
            enrollment.StudentId,
            enrollment.StudentPackageId,
            enrollment.Status,
            enrollment.EnrolledAt,
            enrollment.Attendances.Count,
            enrollment.Attendances.Count(a => a.Status == "present"),
            enrollment.Attendances.Count(a => a.Status == "absent"));
    }
}
