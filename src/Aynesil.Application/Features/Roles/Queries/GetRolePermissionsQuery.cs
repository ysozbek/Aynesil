using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Permissions.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Roles.Queries;

public record GetRolePermissionsQuery(Guid RoleId) : IRequest<IReadOnlyList<PermissionListItemDto>>;

public sealed class GetRolePermissionsQueryHandler
    : IRequestHandler<GetRolePermissionsQuery, IReadOnlyList<PermissionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetRolePermissionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PermissionListItemDto>> Handle(GetRolePermissionsQuery req, CancellationToken ct)
    {
        var exists = await _db.Roles.AnyAsync(r => r.Id == req.RoleId, ct);
        if (!exists) throw new NotFoundException("Role", req.RoleId);

        return await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == req.RoleId)
            .Include(rp => rp.Permission)
            .OrderBy(rp => rp.Permission!.Resource).ThenBy(rp => rp.Permission!.Action)
            .Select(rp => new PermissionListItemDto(
                rp.Permission!.Id, rp.Permission.Code, rp.Permission.Resource,
                rp.Permission.Action, rp.Permission.Description))
            .ToListAsync(ct);
    }
}
