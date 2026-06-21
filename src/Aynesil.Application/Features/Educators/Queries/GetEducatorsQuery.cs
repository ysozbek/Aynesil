using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Educators.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Educators.Queries;

// ── GetEducatorsQuery ─────────────────────────────────────────────────────────

public class GetEducatorsQuery : PagedQuery, IRequest<PaginatedResult<EducatorListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? TitleId { get; set; }
    public Guid? SpecialtyId { get; set; }
    public bool? IsActive { get; set; }
    public string? EmploymentType { get; set; }
}

public sealed class GetEducatorsQueryHandler
    : IRequestHandler<GetEducatorsQuery, PaginatedResult<EducatorListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetEducatorsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<EducatorListItemDto>> Handle(
        GetEducatorsQuery req, CancellationToken ct)
    {
        var q = _db.Educators.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(e => e.CorporationId == req.CorporationId.Value);
        if (req.TitleId.HasValue)
            q = q.Where(e => e.TitleId == req.TitleId.Value);
        if (req.IsActive.HasValue)
            q = q.Where(e => e.IsActive == req.IsActive.Value);
        if (req.EmploymentType is not null)
            q = q.Where(e => e.EmploymentType == req.EmploymentType);
        if (req.CampusId.HasValue)
            q = q.Where(e =>
                e.PrimaryCampusId == req.CampusId.Value ||
                e.Campuses.Any(c => c.CampusId == req.CampusId.Value && c.ActiveTo == null));
        if (req.SpecialtyId.HasValue)
            q = q.Where(e => e.Specialties.Any(s => s.SpecialtyId == req.SpecialtyId.Value));

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim().ToLower();
            q = q.Where(e =>
                (e.FirstName + " " + e.LastName).ToLower().Contains(search) ||
                (e.Email != null && e.Email.ToLower().Contains(search)));
        }

        var query =
            from e in q
            join campus in _db.Campuses.AsNoTracking()
                on e.PrimaryCampusId equals campus.Id into campusGrp
            from campus in campusGrp.DefaultIfEmpty()
            join title in _db.RefValues.AsNoTracking()
                on e.TitleId equals title.Id into titleGrp
            from title in titleGrp.DefaultIfEmpty()
            select new EducatorListItemDto(
                e.Id, e.CorporationId,
                e.FirstName, e.LastName,
                e.FirstName + " " + e.LastName,
                e.TitleId, title != null ? title.Code : null,
                e.Email, e.Phone, e.EmploymentType, e.IsActive,
                e.PrimaryCampusId, campus != null ? campus.Name : null,
                e.Specialties.Count(),
                e.CreatedAt);

        query = req.SortBy?.ToLower() switch
        {
            "lastname"  => req.IsDescending ? query.OrderByDescending(e => e.LastName)  : query.OrderBy(e => e.LastName),
            "createdat" => req.IsDescending ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt),
            "isactive"  => req.IsDescending ? query.OrderByDescending(e => e.IsActive)  : query.OrderBy(e => e.IsActive),
            _           => req.IsDescending ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.LastName)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        return PaginatedResult<EducatorListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetEducatorQuery ──────────────────────────────────────────────────────────

public record GetEducatorQuery(Guid Id) : IRequest<EducatorDto>;

public sealed class GetEducatorQueryHandler : IRequestHandler<GetEducatorQuery, EducatorDto>
{
    private readonly IAppDbContext _db;

    public GetEducatorQueryHandler(IAppDbContext db) => _db = db;

    public async Task<EducatorDto> Handle(GetEducatorQuery req, CancellationToken ct)
        => await EducatorProjection.LoadAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Educator {req.Id} not found.");
}

// ── GetEducatorAvailabilityQuery ──────────────────────────────────────────────

/// <summary>
/// Returns availability profile: active campuses, specialties, and current
/// student-program count. Full scheduling-based availability will be added
/// when the Scheduling module is implemented.
/// </summary>
public record GetEducatorAvailabilityQuery(Guid Id) : IRequest<EducatorAvailabilityDto>;

public sealed class GetEducatorAvailabilityQueryHandler
    : IRequestHandler<GetEducatorAvailabilityQuery, EducatorAvailabilityDto>
{
    private readonly IAppDbContext _db;

    public GetEducatorAvailabilityQueryHandler(IAppDbContext db) => _db = db;

    public async Task<EducatorAvailabilityDto> Handle(
        GetEducatorAvailabilityQuery req, CancellationToken ct)
    {
        var educator = await _db.Educators
            .AsNoTracking()
            .Include(e => e.Campuses)
            .Include(e => e.Specialties)
            .FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Educator {req.Id} not found.");

        var activeCampuses = new List<EducatorCampusDto>();
        foreach (var c in educator.Campuses.Where(c => c.IsActive))
        {
            var name = await _db.Campuses.AsNoTracking()
                .Where(x => x.Id == c.CampusId).Select(x => x.Name).FirstOrDefaultAsync(ct);
            activeCampuses.Add(new EducatorCampusDto(
                c.Id, c.CampusId, name, c.IsPrimary, c.ActiveFrom, c.ActiveTo, c.IsActive));
        }

        var specialties = new List<EducatorSpecialtyDto>();
        foreach (var s in educator.Specialties)
        {
            var label = await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == s.SpecialtyId).Select(r => r.Code).FirstOrDefaultAsync(ct);
            specialties.Add(new EducatorSpecialtyDto(s.Id, s.SpecialtyId, label));
        }

        return new EducatorAvailabilityDto(
            educator.Id,
            $"{educator.FirstName} {educator.LastName}",
            educator.IsActive,
            activeCampuses,
            specialties,
            0);
    }
}

// ── GetEducatorUtilizationQuery ───────────────────────────────────────────────

/// <summary>
/// Returns utilization metrics for an educator or all educators in a corporation.
/// Current implementation counts plan assignments (prepared_by / approved_by).
/// Full utilization based on session counts will be added with the Scheduling module.
/// </summary>
public class GetEducatorUtilizationQuery : IRequest<IReadOnlyList<EducatorUtilizationDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public sealed class GetEducatorUtilizationQueryHandler
    : IRequestHandler<GetEducatorUtilizationQuery, IReadOnlyList<EducatorUtilizationDto>>
{
    private readonly IAppDbContext _db;

    public GetEducatorUtilizationQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EducatorUtilizationDto>> Handle(
        GetEducatorUtilizationQuery req, CancellationToken ct)
    {
        var q = _db.Educators.AsNoTracking()
            .Where(e => e.CorporationId == req.CorporationId);

        if (req.ActiveOnly)
            q = q.Where(e => e.IsActive);

        if (req.CampusId.HasValue)
            q = q.Where(e =>
                e.PrimaryCampusId == req.CampusId.Value ||
                e.Campuses.Any(c => c.CampusId == req.CampusId.Value && c.ActiveTo == null));

        var results = await (
            from e in q
            join campus in _db.Campuses.AsNoTracking()
                on e.PrimaryCampusId equals campus.Id into campusGrp
            from campus in campusGrp.DefaultIfEmpty()
            join title in _db.RefValues.AsNoTracking()
                on e.TitleId equals title.Id into titleGrp
            from title in titleGrp.DefaultIfEmpty()
            select new EducatorUtilizationDto(
                e.Id,
                e.FirstName + " " + e.LastName,
                title != null ? title.Code : null,
                campus != null ? campus.Name : null,
                0,
                0,
                e.Specialties.Count(),
                e.Certifications.Count(c => c.DeletedAt == null))
        ).ToListAsync(ct);

        return results;
    }
}
