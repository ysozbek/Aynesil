using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Plans.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Plans.Queries;

// ── GetEducationPlansQuery ────────────────────────────────────────────────────

public class GetEducationPlansQuery : PagedQuery, IRequest<PaginatedResult<EducationPlanListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? AcademicPeriodId { get; set; }
    public string? Status { get; set; }
    public bool? GuardianVisible { get; set; }
}

public sealed class GetEducationPlansQueryHandler
    : IRequestHandler<GetEducationPlansQuery, PaginatedResult<EducationPlanListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetEducationPlansQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<EducationPlanListItemDto>> Handle(
        GetEducationPlansQuery req, CancellationToken ct)
    {
        var q = _db.EducationPlans.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(p => p.CorporationId == req.CorporationId.Value);
        if (req.StudentId.HasValue)
            q = q.Where(p => p.StudentId == req.StudentId.Value);
        if (req.CampusId.HasValue)
            q = q.Where(p => p.CampusId == req.CampusId.Value);
        if (req.AcademicPeriodId.HasValue)
            q = q.Where(p => p.AcademicPeriodId == req.AcademicPeriodId.Value);
        if (req.Status is not null)
            q = q.Where(p => p.Status == req.Status);
        if (req.GuardianVisible.HasValue)
            q = q.Where(p => p.GuardianVisible == req.GuardianVisible.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(p => p.Title.ToLower().Contains(s));
        }

        var query =
            from p in q
            join student in _db.Students.AsNoTracking()
                on p.StudentId equals student.Id
            join period in _db.AcademicPeriods.AsNoTracking()
                on p.AcademicPeriodId equals period.Id into periodGrp
            from period in periodGrp.DefaultIfEmpty()
            select new EducationPlanListItemDto(
                p.Id, p.StudentId,
                student.FirstName + " " + student.LastName,
                p.AcademicPeriodId, period != null ? period.Name : null,
                p.Title, p.Version, p.Status,
                p.EffectiveFrom, p.EffectiveTo,
                p.GuardianVisible, p.CreatedAt);

        query = req.SortBy?.ToLower() switch
        {
            "status"    => req.IsDescending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            "createdat" => req.IsDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "version"   => req.IsDescending ? query.OrderByDescending(p => p.Version) : query.OrderBy(p => p.Version),
            _           => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<EducationPlanListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetEducationPlanQuery ─────────────────────────────────────────────────────

public record GetEducationPlanQuery(Guid Id) : IRequest<EducationPlanDto>;

public sealed class GetEducationPlanQueryHandler : IRequestHandler<GetEducationPlanQuery, EducationPlanDto>
{
    private readonly IAppDbContext _db;

    public GetEducationPlanQueryHandler(IAppDbContext db) => _db = db;

    public async Task<EducationPlanDto> Handle(GetEducationPlanQuery req, CancellationToken ct)
        => await PlanProjection.LoadAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"EducationPlan {req.Id} not found.");
}

// ── GetGuardianVisiblePlanQuery ───────────────────────────────────────────────

/// <summary>Returns the guardian-accessible (approved, guardian_visible) plan for a student.</summary>
public record GetGuardianVisiblePlanQuery(Guid CorporationId, Guid StudentId) : IRequest<EducationPlanDto?>;

public sealed class GetGuardianVisiblePlanQueryHandler
    : IRequestHandler<GetGuardianVisiblePlanQuery, EducationPlanDto?>
{
    private readonly IAppDbContext _db;

    public GetGuardianVisiblePlanQueryHandler(IAppDbContext db) => _db = db;

    public async Task<EducationPlanDto?> Handle(
        GetGuardianVisiblePlanQuery req, CancellationToken ct)
    {
        var plan = await _db.EducationPlans
            .AsNoTracking()
            .Where(p => p.CorporationId == req.CorporationId
                     && p.StudentId == req.StudentId
                     && p.GuardianVisible
                     && (p.Status == "approved" || p.Status == "active"))
            .OrderByDescending(p => p.ApprovedAt)
            .FirstOrDefaultAsync(ct);

        if (plan is null) return null;

        return await PlanProjection.LoadAsync(_db, plan.Id, ct);
    }
}
