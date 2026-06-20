using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Menus.Dtos;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Shared.Constants;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Menus.Commands;

// ── Request ──────────────────────────────────────────────────────────────────

/// <summary>
/// Replaces all translations for a menu item in one operation (upsert / replace-all strategy).
/// Any locale not included in the request is removed; any new locale is inserted.
/// </summary>
public record SetMenuItemTranslationsCommand(
    Guid MenuItemId,
    IReadOnlyList<SetMenuItemTranslationInput> Translations) : IRequest<IReadOnlyList<MenuItemTranslationDto>>;

public record SetMenuItemTranslationInput(string Locale, string Label);

// ── Validator ─────────────────────────────────────────────────────────────────

public class SetMenuItemTranslationsCommandValidator : AbstractValidator<SetMenuItemTranslationsCommand>
{
    public SetMenuItemTranslationsCommandValidator()
    {
        RuleFor(x => x.MenuItemId).NotEmpty();
        RuleFor(x => x.Translations).NotEmpty().WithMessage("At least one translation is required.");

        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(tr => tr.Locale).NotEmpty().MaximumLength(20);
            t.RuleFor(tr => tr.Label).NotEmpty().MaximumLength(200);
        });

        RuleFor(x => x.Translations)
            .Must(list => list.Select(t => t.Locale).Distinct().Count() == list.Count)
            .WithMessage("Duplicate locale codes are not allowed.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetMenuItemTranslationsCommandHandler
    : IRequestHandler<SetMenuItemTranslationsCommand, IReadOnlyList<MenuItemTranslationDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cache;

    public SetMenuItemTranslationsCommandHandler(
        IAppDbContext db,
        ITenantContext tenantContext,
        ICacheService cache)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task<IReadOnlyList<MenuItemTranslationDto>> Handle(
        SetMenuItemTranslationsCommand req,
        CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var item = await _db.MenuItems
            .Include(m => m.Translations)
            .FirstOrDefaultAsync(m => m.Id == req.MenuItemId, ct)
            ?? throw new NotFoundException("MenuItem", req.MenuItemId);

        if (item.CorporationId.HasValue && item.CorporationId != corporationId)
            throw new UnauthorizedAccessException("You do not have access to this menu item.");

        // Remove all existing translations and replace with the incoming set
        _db.MenuItemTranslations.RemoveRange(item.Translations);
        item.Translations.Clear();

        var newTranslations = req.Translations
            .Select(t => new MenuItemTranslation
            {
                MenuItemId = req.MenuItemId,
                Locale = t.Locale,
                Label = t.Label
            })
            .ToList();

        _db.MenuItemTranslations.AddRange(newTranslations);
        await _db.SaveChangesAsync(ct);

        await _cache.RemoveByPrefixAsync(CacheKeys.ForTenant(corporationId, "menu"), ct);

        return newTranslations
            .OrderBy(t => t.Locale)
            .Select(t => new MenuItemTranslationDto(t.Locale, t.Label))
            .ToList();
    }
}
