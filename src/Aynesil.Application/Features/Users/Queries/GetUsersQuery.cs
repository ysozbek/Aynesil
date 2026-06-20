using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Users.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Users.Queries;

public class GetUsersQuery : PagedQuery, IRequest<PaginatedResult<UserListItemDto>>
{
    /// <summary>Filter by status: 'invited', 'active', 'suspended', 'disabled'.</summary>
    public string? Status { get; set; }

    /// <summary>Filter by users that have this role assigned.</summary>
    public Guid? RoleId { get; set; }

    /// <summary>Filter by users whose primary campus or a scoped role matches this campus.</summary>
    public Guid? CampusId { get; set; }
}

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedResult<UserListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetUsersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<UserListItemDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var query = _db.UserAccounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.ToLowerInvariant();
            query = query.Where(u =>
                u.Username.Contains(term) ||
                u.FullName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)));
        }

        if (req.Status is not null)
            query = query.Where(u => u.Status == req.Status);

        if (req.RoleId.HasValue)
            query = query.Where(u => u.Roles.Any(r => r.RoleId == req.RoleId.Value));

        if (req.CampusId.HasValue)
            query = query.Where(u =>
                u.PrimaryCampusId == req.CampusId.Value ||
                u.Roles.Any(r => r.CampusId == req.CampusId.Value));

        var total = await query.CountAsync(ct);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "username"  => req.IsDescending ? query.OrderByDescending(u => u.Username)  : query.OrderBy(u => u.Username),
            "fullname"  => req.IsDescending ? query.OrderByDescending(u => u.FullName)  : query.OrderBy(u => u.FullName),
            "status"    => req.IsDescending ? query.OrderByDescending(u => u.Status)    : query.OrderBy(u => u.Status),
            "createdat" => req.IsDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _           => query.OrderBy(u => u.FullName)
        };

        var items = await query
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(u => new UserListItemDto(
                u.Id, u.Username, u.Email, u.FullName, u.Status,
                u.PreferredLocale, u.PrimaryCampusId, u.LastLoginAt, u.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<UserListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}
