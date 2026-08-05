using Aynesil.Application.Common.Interfaces;
using Aynesil.Infrastructure.Persistence;
using Aynesil.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Services.Ref;

/// <summary>
/// Resolves effective reference values for the current tenant.
/// Mirrors the logic of the DB view ref.v_effective_ref_value:
///   1. System/global values (corporation_id IS NULL)
///   2. Tenant-specific values (corporation_id = current tenant)
///   3. Apply tenant overrides (ref_value_tenant_override)
/// Cached per (corporation_id, type_code) for 30 minutes (all values; activeOnly filtered after cache).
/// </summary>
public sealed class RefDataService : IRefDataService
{
    private readonly AynesilDbContext _db;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public RefDataService(
        AynesilDbContext db,
        ICacheService cache,
        ITenantContext tenantContext)
    {
        _db = db;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<RefValueDto>> GetValuesAsync(
        string typeCode,
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var corpId = _tenantContext.CorporationId;
        var locale = _tenantContext.Locale ?? "tr";
        var cacheKey = CacheKeys.RefValues(corpId, typeCode);

        var all = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            var values = await _db.RefValues
                .AsNoTracking()
                .Include(v => v.TenantOverrides.Where(o => o.CorporationId == corpId))
                .Include(v => v.Translations)
                .Where(v => v.RefType!.Code == typeCode)
                .Where(v => v.CorporationId == null || v.CorporationId == corpId)
                .ToListAsync(ct);

            var result = values
                .Select(v =>
                {
                    var ovr = v.TenantOverrides.FirstOrDefault();
                    var isActive = ovr?.IsActive ?? v.IsActive;

                    var label = v.Translations.FirstOrDefault(t => t.Locale == locale)?.Label
                        ?? v.Translations.FirstOrDefault(t => t.Locale == "tr")?.Label
                        ?? v.Code;

                    return new RefValueDto(
                        v.Id, v.Code, label,
                        v.Translations.FirstOrDefault(t => t.Locale == locale)?.ShortLabel,
                        null,
                        ovr?.SortOrder ?? v.SortOrder,
                        ovr?.IsDefault ?? v.IsDefault,
                        v.IsSystem,
                        v.Metadata,
                        isActive,
                        IsTenantOwned: v.CorporationId == corpId && corpId.HasValue);
                })
                .OrderBy(v => v.SortOrder)
                .ToList();

            return (IReadOnlyList<RefValueDto>)result;
        }, TimeSpan.FromMinutes(30), ct);

        if (activeOnly)
            return all.Where(v => v.IsActive).ToList();

        return all;
    }

    public async Task<RefValueDto?> GetDefaultAsync(string typeCode, CancellationToken ct = default)
    {
        var values = await GetValuesAsync(typeCode, ct: ct);
        return values.FirstOrDefault(v => v.IsDefault) ?? values.FirstOrDefault();
    }

    public async Task<RefValueDto?> GetByCodeAsync(string typeCode, string code, CancellationToken ct = default)
    {
        var values = await GetValuesAsync(typeCode, ct: ct);
        return values.FirstOrDefault(v => v.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<RefValueDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var locale = _tenantContext.Locale ?? "tr";
        var corpId = _tenantContext.CorporationId;

        var v = await _db.RefValues
            .AsNoTracking()
            .Include(v => v.Translations)
            .Include(v => v.TenantOverrides.Where(o => o.CorporationId == corpId))
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (v is null) return null;

        var ovr = v.TenantOverrides.FirstOrDefault();
        var label = v.Translations.FirstOrDefault(t => t.Locale == locale)?.Label
            ?? v.Translations.FirstOrDefault(t => t.Locale == "tr")?.Label
            ?? v.Code;

        return new RefValueDto(
            v.Id, v.Code, label, null, null,
            ovr?.SortOrder ?? v.SortOrder,
            ovr?.IsDefault ?? v.IsDefault,
            v.IsSystem,
            v.Metadata,
            ovr?.IsActive ?? v.IsActive,
            IsTenantOwned: v.CorporationId == corpId && corpId.HasValue);
    }
}
