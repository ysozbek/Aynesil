using System.Text.Json;
using Aynesil.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Settings.Queries;

/// <summary>
/// Returns effective settings for the current tenant as a flat key → value map.
/// Used by the shell on every authenticated page load.
/// </summary>
public sealed record GetEffectiveSettingsQuery : IRequest<IReadOnlyDictionary<string, JsonElement?>>;

public sealed class GetEffectiveSettingsQueryHandler
    : IRequestHandler<GetEffectiveSettingsQuery, IReadOnlyDictionary<string, JsonElement?>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public GetEffectiveSettingsQueryHandler(
        IAppDbContext db,
        ITenantContext tenant,
        ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyDictionary<string, JsonElement?>> Handle(
        GetEffectiveSettingsQuery request,
        CancellationToken ct)
    {
        var definitions = await _db.SettingDefinitions
            .AsNoTracking()
            .Select(d => new { d.Key, d.DefaultValue })
            .ToListAsync(ct);

        if (definitions.Count == 0)
            return new Dictionary<string, JsonElement?>();

        var corporationId = _tenant.CorporationId;
        var userId = _currentUser.UserId;

        var values = await _db.SettingValues
            .AsNoTracking()
            .Where(v =>
                (v.ScopeLevel == "system" && v.CorporationId == null)
                || (corporationId.HasValue
                    && v.ScopeLevel == "corporation"
                    && v.CorporationId == corporationId
                    && v.ScopeId == null)
                || (corporationId.HasValue && userId.HasValue
                    && v.ScopeLevel == "user"
                    && v.CorporationId == corporationId
                    && v.ScopeId == userId))
            .Select(v => new { v.SettingKey, v.ScopeLevel, v.Value })
            .ToListAsync(ct);

        var byKey = values
            .GroupBy(v => v.SettingKey)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new Dictionary<string, JsonElement?>(definitions.Count);

        foreach (var def in definitions)
        {
            string? raw = null;
            if (byKey.TryGetValue(def.Key, out var scoped))
            {
                raw = scoped.FirstOrDefault(v => v.ScopeLevel == "user")?.Value
                    ?? scoped.FirstOrDefault(v => v.ScopeLevel == "corporation")?.Value
                    ?? scoped.FirstOrDefault(v => v.ScopeLevel == "system")?.Value;
            }

            raw ??= def.DefaultValue;
            result[def.Key] = ParseJson(raw);
        }

        return result;
    }

    private static JsonElement? ParseJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(raw);
        }
    }
}
