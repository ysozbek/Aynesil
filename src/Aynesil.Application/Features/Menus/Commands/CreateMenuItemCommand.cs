using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Menus.Dtos;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Shared.Constants;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Menus.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

public record CreateMenuItemCommand(
    Guid? ParentId,
    string Code,
    string? Route,
    string? Icon,
    int SortOrder,
    Guid? RequiredPermissionId,
    string? FeatureFlag,
    IReadOnlyList<CreateMenuItemTranslationInput> Translations) : IRequest<MenuItemListItemDto>;

public record CreateMenuItemTranslationInput(string Locale, string Label);

// ── Validator ─────────────────────────────────────────────────────────────────

public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-z0-9][a-z0-9._\-]*$")
            .WithMessage("Code must start with a letter or digit and contain only lowercase letters, digits, dots, hyphens, or underscores.");

        RuleFor(x => x.Route).MaximumLength(300).When(x => x.Route is not null);
        RuleFor(x => x.Icon).MaximumLength(100).When(x => x.Icon is not null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FeatureFlag).MaximumLength(100).When(x => x.FeatureFlag is not null);
        RuleFor(x => x.Translations).NotNull();

        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(tr => tr.Locale).NotEmpty().MaximumLength(20);
            t.RuleFor(tr => tr.Label).NotEmpty().MaximumLength(200);
        });
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateMenuItemCommandHandler : IRequestHandler<CreateMenuItemCommand, MenuItemListItemDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cache;

    public CreateMenuItemCommandHandler(
        IAppDbContext db,
        ITenantContext tenantContext,
        ICacheService cache)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task<MenuItemListItemDto> Handle(CreateMenuItemCommand req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        if (req.ParentId.HasValue)
        {
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

        var codeLower = req.Code.Trim().ToLowerInvariant().Replace(' ', '-');
        var codeTaken = await _db.MenuItems.AnyAsync(
            m => m.CorporationId == corporationId && m.Code == codeLower, ct);
        if (codeTaken)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.Code), $"Menu item code '{codeLower}' already exists in this organization.")]);

        var item = MenuItem.Create(
            corporationId,
            req.ParentId,
            codeLower,
            req.Route,
            req.Icon,
            req.SortOrder,
            req.RequiredPermissionId,
            req.FeatureFlag);

        foreach (var t in req.Translations)
            item.Translations.Add(new MenuItemTranslation
            {
                MenuItemId = item.Id,
                Locale = t.Locale,
                Label = t.Label
            });

        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync(ct);

        await InvalidateMenuCacheAsync(corporationId, ct);

        // Re-fetch with navigations for a complete response DTO
        var created = await _db.MenuItems
            .AsNoTracking()
            .Include(m => m.Translations)
            .Include(m => m.RequiredPermission)
            .FirstAsync(m => m.Id == item.Id, ct);

        return created.ToListItemDto();
    }

    private Task InvalidateMenuCacheAsync(Guid corporationId, CancellationToken ct) =>
        _cache.RemoveByPrefixAsync(CacheKeys.ForTenant(corporationId, "menu"), ct);
}
