using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Permissions.Dtos;
using MediatR;

namespace Aynesil.Application.Features.Permissions.Queries;

public record GetPermissionQuery(Guid PermissionId) : IRequest<PermissionDto>;

public sealed class GetPermissionQueryHandler : IRequestHandler<GetPermissionQuery, PermissionDto>
{
    private readonly IAppDbContext _db;

    public GetPermissionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PermissionDto> Handle(GetPermissionQuery req, CancellationToken ct)
    {
        var permission = await _db.Permissions.FindAsync([req.PermissionId], ct)
            ?? throw new NotFoundException("Permission", req.PermissionId);

        return permission.ToDto();
    }
}
