using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Ref.Entities;
using Aynesil.Shared.Constants;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Aynesil.Application.Features.RefData.Commands;

// ── Create ────────────────────────────────────────────────────────────────────

public record CreateRefValueCommand(
    string TypeCode,
    string Code,
    string Label,
    int SortOrder = 0,
    bool IsDefault = false) : IRequest<RefValueDto>;

public partial class CreateRefValueCommandValidator : AbstractValidator<CreateRefValueCommand>
{
    public CreateRefValueCommandValidator()
    {
        RuleFor(x => x.TypeCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Must(c => CodePattern().IsMatch(c.Trim().ToLowerInvariant()))
            .WithMessage("Code must be lowercase snake_case (letters, digits, underscores).");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex CodePattern();
}

public sealed class CreateRefValueCommandHandler : IRequestHandler<CreateRefValueCommand, RefValueDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public CreateRefValueCommandHandler(
        IAppDbContext db,
        ITenantContext tenant,
        ICurrentUserService currentUser,
        ICacheService cache)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<RefValueDto> Handle(CreateRefValueCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var typeCode = NormalizeTypeCode(req.TypeCode);
        var code = req.Code.Trim().ToLowerInvariant();
        var locale = _tenant.Locale ?? "tr";

        var refType = await _db.RefTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == typeCode, ct)
            ?? throw new NotFoundException("RefType", typeCode);

        if (!refType.AllowsTenantValues)
            throw new InvalidOperationException(
                $"Reference type '{typeCode}' does not allow tenant-specific values.");

        var codeExists = await _db.RefValues.AnyAsync(
            v => v.RefTypeId == refType.Id
                 && v.CorporationId == corporationId
                 && v.Code == code, ct);
        if (codeExists)
            throw new InvalidOperationException(
                $"A value with code '{code}' already exists for this corporation in '{typeCode}'.");

        if (req.IsDefault)
            await ClearTenantDefaultsAsync(refType.Id, corporationId, excludeId: null, ct);

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser.UserId;

        var value = new RefValue
        {
            RefTypeId = refType.Id,
            CorporationId = corporationId,
            Code = code,
            SortOrder = req.SortOrder,
            IsActive = true,
            IsDefault = req.IsDefault,
            IsSystem = false,
            Metadata = "{}",
            CreatedAt = now,
            CreatedBy = userId,
            UpdatedAt = now,
            UpdatedBy = userId,
        };

        _db.RefValues.Add(value);
        _db.RefValueTranslations.Add(new RefValueTranslation
        {
            RefValueId = value.Id,
            Locale = locale,
            Label = req.Label.Trim(),
        });

        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.RefValues(corporationId, typeCode), ct);

        return new RefValueDto(
            value.Id, value.Code, req.Label.Trim(), null, null,
            value.SortOrder, value.IsDefault, value.IsSystem, value.Metadata,
            value.IsActive, IsTenantOwned: true);
    }

    private async Task ClearTenantDefaultsAsync(
        Guid refTypeId, Guid corporationId, Guid? excludeId, CancellationToken ct)
    {
        var defaults = await _db.RefValues
            .Where(v => v.RefTypeId == refTypeId
                        && v.CorporationId == corporationId
                        && v.IsDefault
                        && (excludeId == null || v.Id != excludeId))
            .ToListAsync(ct);

        foreach (var d in defaults)
        {
            d.IsDefault = false;
            d.UpdatedAt = DateTimeOffset.UtcNow;
            d.UpdatedBy = _currentUser.UserId;
        }
    }

    private static string NormalizeTypeCode(string typeCode) =>
        typeCode.Trim().ToLowerInvariant().Replace('-', '_');
}

// ── Update ────────────────────────────────────────────────────────────────────

public record UpdateRefValueCommand(
    Guid Id,
    string Label,
    int SortOrder = 0,
    bool IsDefault = false) : IRequest<RefValueDto>;

public class UpdateRefValueCommandValidator : AbstractValidator<UpdateRefValueCommand>
{
    public UpdateRefValueCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateRefValueCommandHandler : IRequestHandler<UpdateRefValueCommand, RefValueDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public UpdateRefValueCommandHandler(
        IAppDbContext db,
        ITenantContext tenant,
        ICurrentUserService currentUser,
        ICacheService cache)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<RefValueDto> Handle(UpdateRefValueCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var locale = _tenant.Locale ?? "tr";

        var value = await _db.RefValues
            .Include(v => v.Translations)
            .Include(v => v.RefType)
            .FirstOrDefaultAsync(v => v.Id == req.Id, ct)
            ?? throw new NotFoundException("RefValue", req.Id);

        var isTenantOwned = value.CorporationId == corporationId;
        var isSharedGlobal = value.CorporationId is null;

        // Other tenants' rows are never editable.
        if (!isTenantOwned && !isSharedGlobal)
            throw new UnauthorizedAccessException("You do not have access to this reference value.");

        // Platform-owned system vocabulary is immutable; use activate/deactivate override instead.
        if (value.IsSystem)
            throw new InvalidOperationException(
                "System reference values cannot be edited. Use activate/deactivate to override.");

        if (req.IsDefault && !value.IsDefault)
        {
            // Clear other defaults in the same scope (tenant-owned or global).
            var scopeCorpId = value.CorporationId;
            var defaults = await _db.RefValues
                .Where(v => v.RefTypeId == value.RefTypeId
                            && v.CorporationId == scopeCorpId
                            && v.IsDefault
                            && v.Id != value.Id)
                .ToListAsync(ct);
            foreach (var d in defaults)
            {
                d.IsDefault = false;
                d.UpdatedAt = DateTimeOffset.UtcNow;
                d.UpdatedBy = _currentUser.UserId;
            }
        }

        value.SortOrder = req.SortOrder;
        value.IsDefault = req.IsDefault;
        value.UpdatedAt = DateTimeOffset.UtcNow;
        value.UpdatedBy = _currentUser.UserId;

        var translation = value.Translations.FirstOrDefault(t => t.Locale == locale);
        if (translation is null)
        {
            translation = new RefValueTranslation
            {
                RefValueId = value.Id,
                Locale = locale,
                Label = req.Label.Trim(),
            };
            _db.RefValueTranslations.Add(translation);
        }
        else
        {
            translation.Label = req.Label.Trim();
        }

        await _db.SaveChangesAsync(ct);

        var typeCode = value.RefType!.Code;
        await _cache.RemoveAsync(CacheKeys.RefValues(corporationId, typeCode), ct);

        return new RefValueDto(
            value.Id, value.Code, translation.Label, translation.ShortLabel, null,
            value.SortOrder, value.IsDefault, value.IsSystem, value.Metadata,
            value.IsActive, IsTenantOwned: isTenantOwned);
    }
}

// ── Set Active ────────────────────────────────────────────────────────────────

public record SetRefValueActiveCommand(Guid Id, bool IsActive) : IRequest;

public class SetRefValueActiveCommandValidator : AbstractValidator<SetRefValueActiveCommand>
{
    public SetRefValueActiveCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class SetRefValueActiveCommandHandler : IRequestHandler<SetRefValueActiveCommand>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public SetRefValueActiveCommandHandler(
        IAppDbContext db,
        ITenantContext tenant,
        ICurrentUserService currentUser,
        ICacheService cache)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task Handle(SetRefValueActiveCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var value = await _db.RefValues
            .Include(v => v.RefType)
            .FirstOrDefaultAsync(v => v.Id == req.Id, ct)
            ?? throw new NotFoundException("RefValue", req.Id);

        // Tenant may only touch own rows or shared (corporation_id null) rows visible to them.
        if (value.CorporationId is not null && value.CorporationId != corporationId)
            throw new UnauthorizedAccessException("You do not have access to this reference value.");

        if (value.CorporationId == corporationId)
        {
            value.IsActive = req.IsActive;
            value.UpdatedAt = DateTimeOffset.UtcNow;
            value.UpdatedBy = _currentUser.UserId;
        }
        else
        {
            // Shared/global: upsert tenant override
            var ovr = await _db.RefValueTenantOverrides
                .FirstOrDefaultAsync(
                    o => o.CorporationId == corporationId && o.RefValueId == value.Id, ct);

            if (ovr is null)
            {
                _db.RefValueTenantOverrides.Add(new RefValueTenantOverride
                {
                    CorporationId = corporationId,
                    RefValueId = value.Id,
                    IsActive = req.IsActive,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    UpdatedBy = _currentUser.UserId,
                });
            }
            else
            {
                ovr.IsActive = req.IsActive;
                ovr.UpdatedAt = DateTimeOffset.UtcNow;
                ovr.UpdatedBy = _currentUser.UserId;
            }
        }

        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.RefValues(corporationId, value.RefType!.Code), ct);
    }
}
