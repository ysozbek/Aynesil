using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Permissions.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Permissions.Queries;

public class GetPermissionsQuery : PagedQuery, IRequest<PaginatedResult<PermissionListItemDto>>
{
    /// <summary>Filter by resource name: 'student', 'session', 'report', etc.</summary>
    public string? Resource { get; set; }
}

public sealed class GetPermissionsQueryHandler
    : IRequestHandler<GetPermissionsQuery, PaginatedResult<PermissionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetPermissionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<PermissionListItemDto>> Handle(GetPermissionsQuery req, CancellationToken ct)
    {
        var query = _db.Permissions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Resource))
            query = query.Where(p => p.Resource == req.Resource.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.ToLowerInvariant();
            query = query.Where(p => p.Code.Contains(term) || p.Resource.Contains(term) || p.Action.Contains(term));
        }

        var total = await query.CountAsync(ct);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "code"     => req.IsDescending ? query.OrderByDescending(p => p.Code)     : query.OrderBy(p => p.Code),
            "resource" => req.IsDescending ? query.OrderByDescending(p => p.Resource) : query.OrderBy(p => p.Resource),
            _          => query.OrderBy(p => p.Resource).ThenBy(p => p.Action)
        };

        var items = await query
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(p => new PermissionListItemDto(p.Id, p.Code, p.Resource, p.Action, p.Description))
            .ToListAsync(ct);

        return PaginatedResult<PermissionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}
