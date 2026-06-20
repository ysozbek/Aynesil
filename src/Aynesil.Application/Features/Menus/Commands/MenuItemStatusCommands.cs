using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared.Constants;
using FluentValidation;
using MediatR;

namespace Aynesil.Application.Features.Menus.Commands;

// ── Activate ─────────────────────────────────────────────────────────────────

public record ActivateMenuItemCommand(Guid Id) : IRequest;

public class ActivateMenuItemCommandValidator : AbstractValidator<ActivateMenuItemCommand>
{
    public ActivateMenuItemCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class ActivateMenuItemCommandHandler : IRequestHandler<ActivateMenuItemCommand>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cache;

    public ActivateMenuItemCommandHandler(
        IAppDbContext db,
        ITenantContext tenantContext,
        ICacheService cache)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task Handle(ActivateMenuItemCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var item = await _db.MenuItems.FindAsync([req.Id], ct)
            ?? throw new NotFoundException("MenuItem", req.Id);

        if (item.CorporationId.HasValue && item.CorporationId != corporationId)
            throw new UnauthorizedAccessException("You do not have access to this menu item.");

        if (item.IsActive) return; // idempotent

        item.Activate();
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveByPrefixAsync(CacheKeys.ForTenant(corporationId, "menu"), ct);
    }
}

// ── Deactivate ────────────────────────────────────────────────────────────────

public record DeactivateMenuItemCommand(Guid Id) : IRequest;

public class DeactivateMenuItemCommandValidator : AbstractValidator<DeactivateMenuItemCommand>
{
    public DeactivateMenuItemCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeactivateMenuItemCommandHandler : IRequestHandler<DeactivateMenuItemCommand>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cache;

    public DeactivateMenuItemCommandHandler(
        IAppDbContext db,
        ITenantContext tenantContext,
        ICacheService cache)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task Handle(DeactivateMenuItemCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var item = await _db.MenuItems.FindAsync([req.Id], ct)
            ?? throw new NotFoundException("MenuItem", req.Id);

        if (item.CorporationId.HasValue && item.CorporationId != corporationId)
            throw new UnauthorizedAccessException("You do not have access to this menu item.");

        if (!item.IsActive) return; // idempotent

        item.Deactivate();
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveByPrefixAsync(CacheKeys.ForTenant(corporationId, "menu"), ct);
    }
}
