using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared.Constants;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Menus.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

public record DeleteMenuItemCommand(Guid Id) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public class DeleteMenuItemCommandValidator : AbstractValidator<DeleteMenuItemCommand>
{
    public DeleteMenuItemCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class DeleteMenuItemCommandHandler : IRequestHandler<DeleteMenuItemCommand>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cache;

    public DeleteMenuItemCommandHandler(
        IAppDbContext db,
        ITenantContext tenantContext,
        ICacheService cache)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task Handle(DeleteMenuItemCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var item = await _db.MenuItems.FindAsync([req.Id], ct)
            ?? throw new NotFoundException("MenuItem", req.Id);

        if (item.CorporationId.HasValue && item.CorporationId != corporationId)
            throw new UnauthorizedAccessException("You do not have access to this menu item.");

        item.EnsureCanBeDeleted();

        var hasChildren = await _db.MenuItems.AnyAsync(m => m.ParentId == req.Id, ct);
        if (hasChildren)
            throw new InvalidOperationException(
                "Cannot delete a menu item that has children. Remove or re-parent the children first.");

        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveByPrefixAsync(CacheKeys.ForTenant(corporationId, "menu"), ct);
    }
}
