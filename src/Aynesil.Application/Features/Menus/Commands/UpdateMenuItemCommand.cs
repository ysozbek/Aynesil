using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Menus.Dtos;
using Aynesil.Shared.Constants;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Menus.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

public record UpdateMenuItemCommand(
    Guid Id,
    Guid? ParentId,
    string? Route,
    string? Icon,
    int SortOrder,
    Guid? RequiredPermissionId,
    string? FeatureFlag,
    int RowVersion) : IRequest<MenuItemListItemDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class UpdateMenuItemCommandValidator : AbstractValidator<UpdateMenuItemCommand>
{
    public UpdateMenuItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Route).MaximumLength(300).When(x => x.Route is not null);
        RuleFor(x => x.Icon).MaximumLength(100).When(x => x.Icon is not null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FeatureFlag).MaximumLength(100).When(x => x.FeatureFlag is not null);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateMenuItemCommandHandler : IRequestHandler<UpdateMenuItemCommand, MenuItemListItemDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cache;

    public UpdateMenuItemCommandHandler(
        IAppDbContext db,
        ITenantContext tenantContext,
        ICacheService cache)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task<MenuItemListItemDto> Handle(UpdateMenuItemCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var item = await _db.MenuItems
            .Include(m => m.Translations)
            .Include(m => m.RequiredPermission)
            .FirstOrDefaultAsync(m => m.Id == req.Id, ct)
            ?? throw new NotFoundException("MenuItem", req.Id);

        // Tenants may update their own items and platform defaults (e.g. reorder)
        if (item.CorporationId.HasValue && item.CorporationId != corporationId)
            throw new UnauthorizedAccessException("You do not have access to this menu item.");

        if (req.RowVersion != item.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The record was modified by another user. Please reload and try again.")]);

        if (req.ParentId.HasValue)
        {
            if (req.ParentId == req.Id)
                throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                    nameof(req.ParentId), "A menu item cannot be its own parent.")]);

            var parentExists = await _db.MenuItems.AnyAsync(
                m => m.Id == req.ParentId &&
                     (m.CorporationId == null || m.CorporationId == corporationId), ct);
            if (!parentExists)
                throw new NotFoundException("MenuItem", req.ParentId.Value);
        }

        if (req.RequiredPermissionId.HasValue)
        {
            var permExists = await _db.Permissions.AnyAsync(
                p => p.Id == req.RequiredPermissionId, ct);
            if (!permExists)
                throw new NotFoundException("Permission", req.RequiredPermissionId.Value);
        }

        item.Update(
            req.ParentId,
            req.Route,
            req.Icon,
            req.SortOrder,
            req.RequiredPermissionId,
            req.FeatureFlag);

        await _db.SaveChangesAsync(ct);
        await _cache.RemoveByPrefixAsync(CacheKeys.ForTenant(corporationId, "menu"), ct);

        return item.ToListItemDto();
    }
}
