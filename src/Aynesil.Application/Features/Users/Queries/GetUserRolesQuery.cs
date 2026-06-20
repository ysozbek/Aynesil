using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Users.Queries;

public record GetUserRolesQuery(Guid UserId) : IRequest<IReadOnlyList<UserRoleDto>>;

public sealed class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, IReadOnlyList<UserRoleDto>>
{
    private readonly IAppDbContext _db;

    public GetUserRolesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserRoleDto>> Handle(GetUserRolesQuery req, CancellationToken ct)
    {
        var exists = await _db.UserAccounts.AnyAsync(u => u.Id == req.UserId, ct);
        if (!exists) throw new NotFoundException("User", req.UserId);

        return await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == req.UserId)
            .Include(ur => ur.Role)
            .OrderBy(ur => ur.Role!.Name)
            .Select(ur => new UserRoleDto(
                ur.Id, ur.RoleId,
                ur.Role!.Code, ur.Role.Name,
                ur.CampusId, ur.ValidFrom, ur.ValidTo, ur.CreatedAt))
            .ToListAsync(ct);
    }
}
