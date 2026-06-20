using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Permissions.Dtos;
using Aynesil.Application.Features.Roles.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Roles.Queries;

public record GetRoleQuery(Guid RoleId) : IRequest<RoleDto>;

public sealed class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, RoleDto>
{
    private readonly IAppDbContext _db;

    public GetRoleQueryHandler(IAppDbContext db) => _db = db;

    public async Task<RoleDto> Handle(GetRoleQuery req, CancellationToken ct)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == req.RoleId, ct)
            ?? throw new NotFoundException("Role", req.RoleId);

        var permissions = role.RolePermissions
            .Select(rp => rp.Permission!.ToListItemDto())
            .OrderBy(p => p.Resource).ThenBy(p => p.Action)
            .ToList();

        return role.ToDto(permissions);
    }
}
