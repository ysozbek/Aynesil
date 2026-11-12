using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Queries;

// ── GetGuardiansQuery ─────────────────────────────────────────────────────────

public class GetGuardiansQuery : PagedQuery, IRequest<PaginatedResult<GuardianListItemDto>>
{
    public bool? HasPortalAccount { get; set; }
}

public sealed class GetGuardiansQueryHandler
    : IRequestHandler<GetGuardiansQuery, PaginatedResult<GuardianListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetGuardiansQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResult<GuardianListItemDto>> Handle(
        GetGuardiansQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var baseQuery = _db.Guardians.AsNoTracking();

        baseQuery = baseQuery.Where(g => g.CorporationId == corporationId);

        if (req.HasPortalAccount.HasValue)
            baseQuery = req.HasPortalAccount.Value
                ? baseQuery.Where(g => g.UserId != null)
                : baseQuery.Where(g => g.UserId == null);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(g =>
                (g.FirstName + " " + g.LastName).ToLower().Contains(search) ||
                (g.Email != null && g.Email.ToLower().Contains(search)) ||
                (g.Phone != null && g.Phone.Contains(search)));
        }

        // Sıralama projeksiyondan önce entity alanı üzerinden yapılır.
        baseQuery = req.IsDescending
            ? baseQuery.OrderByDescending(g => g.LastName)
            : baseQuery.OrderBy(g => g.LastName);

        var query = from g in baseQuery
                    select new GuardianListItemDto(
                        g.Id, g.FirstName, g.LastName,
                        g.FirstName + " " + g.LastName,
                        g.Email, g.Phone,
                        g.UserId != null,
                        _db.StudentGuardians.Count(sg => sg.GuardianId == g.Id));

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        return PaginatedResult<GuardianListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetGuardianQuery ──────────────────────────────────────────────────────────

public record GetGuardianQuery(Guid Id) : IRequest<GuardianDto>;

public sealed class GetGuardianQueryHandler : IRequestHandler<GetGuardianQuery, GuardianDto>
{
    private readonly IAppDbContext _db;

    public GetGuardianQueryHandler(IAppDbContext db) => _db = db;

    public async Task<GuardianDto> Handle(GetGuardianQuery req, CancellationToken ct)
        => await StudentProjection.LoadGuardianAsync(_db, req.Id, ct)
           ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Guardian"));
}

// ── GetStudentGuardiansQuery ──────────────────────────────────────────────────

public record GetStudentGuardiansQuery(Guid StudentId)
    : IRequest<IReadOnlyList<StudentGuardianDto>>;

public sealed class GetStudentGuardiansQueryHandler
    : IRequestHandler<GetStudentGuardiansQuery, IReadOnlyList<StudentGuardianDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentGuardiansQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StudentGuardianDto>> Handle(
        GetStudentGuardiansQuery req, CancellationToken ct)
    {
        var links = await _db.StudentGuardians
            .AsNoTracking()
            .Where(sg => sg.StudentId == req.StudentId)
            .ToListAsync(ct);

        var studentFullName = await _db.Students
            .AsNoTracking()
            .Where(s => s.Id == req.StudentId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var result = new List<StudentGuardianDto>();

        foreach (var link in links)
        {
            var guardian = await _db.Guardians
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == link.GuardianId, ct);

            var relLabel = link.RelationshipId.HasValue
                ? await _db.RefValues.AsNoTracking()
                    .Where(r => r.Id == link.RelationshipId.Value)
                    .Select(r => r.Code)
                    .FirstOrDefaultAsync(ct)
                : null;

            result.Add(new StudentGuardianDto(
                link.Id, link.GuardianId,
                guardian is not null ? $"{guardian.FirstName} {guardian.LastName}" : string.Empty,
                guardian?.Email, guardian?.Phone,
                link.StudentId, studentFullName,
                link.RelationshipId, relLabel,
                link.IsPrimary, link.HasCustody, link.PortalAccess, link.FinancialResponsible));
        }

        return result;
    }
}
