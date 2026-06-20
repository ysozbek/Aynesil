using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Roles.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Roles.Queries;

public class GetRolesQuery : PagedQuery, IRequest<PaginatedResult<RoleListItemDto>>
{
    /// <summary>When true, includes platform system role templates alongside tenant-specific roles.</summary>
    public bool IncludeSystem { get; set; } = true;
}

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, PaginatedResult<RoleListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetRolesQueryHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PaginatedResult<RoleListItemDto>> Handle(GetRolesQuery req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId;

        // Return tenant-scoped roles + optionally system templates
        var query = _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .Where(r => r.CorporationId == corporationId ||
                        (req.IncludeSystem && r.CorporationId == null));

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.ToLowerInvariant();
            query = query.Where(r => r.Name.Contains(term) || r.Code.Contains(term));
        }

        var total = await query.CountAsync(ct);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "code"      => req.IsDescending ? query.OrderByDescending(r => r.Code) : query.OrderBy(r => r.Code),
            "name"      => req.IsDescending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
            "createdat" => req.IsDescending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
            _           => query.OrderBy(r => r.Name)
        };

        var items = await query
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(r => new RoleListItemDto(
                r.Id, r.CorporationId, r.Code, r.Name, r.Description,
                r.IsSystem, r.RolePermissions.Count, r.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<RoleListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}
