using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Menus.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Menus.Queries;

public record GetMenuItemQuery(Guid Id) : IRequest<MenuItemDto>;

public sealed class GetMenuItemQueryHandler : IRequestHandler<GetMenuItemQuery, MenuItemDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetMenuItemQueryHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<MenuItemDto> Handle(GetMenuItemQuery req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var item = await _db.MenuItems
            .AsNoTracking()
            .Include(m => m.Translations)
            .Include(m => m.RequiredPermission)
            .FirstOrDefaultAsync(
                m => m.Id == req.Id &&
                     (m.CorporationId == null || m.CorporationId == corporationId), ct)
            ?? throw new NotFoundException("MenuItem", req.Id);

        return item.ToDto();
    }
}
