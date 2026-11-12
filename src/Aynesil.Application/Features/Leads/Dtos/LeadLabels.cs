using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.RefData;

namespace Aynesil.Application.Features.Leads.Dtos;

/// <summary>
/// CRM DTO'larındaki ref_value kimliklerini kullanıcı dilindeki adlara çevirir
/// (kaynak, durum, hat aşaması, aktivite türü). Her çağrı tek bir RefLabelLoader sorgusu yapar;
/// pasifleştirilmiş değerler de çözülür.
/// </summary>
internal static class LeadLabels
{
    internal static async Task<IReadOnlyList<LeadDto>> ApplyAsync(
        IAppDbContext db, IReadOnlyList<LeadDto> items, string? locale, CancellationToken ct)
    {
        var names = await LoadAsync(db, locale, ct,
            items.SelectMany(i => new[] { i.SourceId, i.StatusId, i.PipelineStageId }));
        return items.Select(i => i with
        {
            SourceName = Name(names, i.SourceId),
            StatusName = Name(names, i.StatusId),
            PipelineStageName = Name(names, i.PipelineStageId),
        }).ToList();
    }

    internal static async Task<List<LeadListItemDto>> ApplyAsync(
        IAppDbContext db, List<LeadListItemDto> items, string? locale, CancellationToken ct)
    {
        var names = await LoadAsync(db, locale, ct,
            items.SelectMany(i => new[] { i.SourceId, i.StatusId, i.PipelineStageId }));
        return items.Select(i => i with
        {
            SourceName = Name(names, i.SourceId),
            StatusName = Name(names, i.StatusId),
            PipelineStageName = Name(names, i.PipelineStageId),
        }).ToList();
    }

    internal static async Task<List<LeadActivityDto>> ApplyAsync(
        IAppDbContext db, List<LeadActivityDto> items, string? locale, CancellationToken ct)
    {
        var names = await LoadAsync(db, locale, ct, items.Select(i => i.ActivityTypeId));
        return items.Select(i => i with { ActivityTypeName = Name(names, i.ActivityTypeId) }).ToList();
    }

    internal static async Task<List<LeadStatusHistoryDto>> ApplyAsync(
        IAppDbContext db, List<LeadStatusHistoryDto> items, string? locale, CancellationToken ct)
    {
        var names = await LoadAsync(db, locale, ct, items.SelectMany(i => new[] { i.StatusId, i.PipelineStageId }));
        return items.Select(i => i with
        {
            StatusName = Name(names, i.StatusId),
            PipelineStageName = Name(names, i.PipelineStageId),
        }).ToList();
    }

    internal static async Task<List<PipelineStageCountDto>> ApplyAsync(
        IAppDbContext db, List<PipelineStageCountDto> items, string? locale, CancellationToken ct)
    {
        var names = await LoadAsync(db, locale, ct, items.Select(i => (Guid?)i.StageId));
        return items.Select(i => i with { StageName = Name(names, i.StageId) }).ToList();
    }

    private static Task<Dictionary<Guid, string>> LoadAsync(
        IAppDbContext db, string? locale, CancellationToken ct, IEnumerable<Guid?> ids)
        => RefLabelLoader.LoadAsync(
            db, ids.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList(), locale, ct);

    private static string? Name(Dictionary<Guid, string> names, Guid? id)
        => id.HasValue ? names.GetValueOrDefault(id.Value) : null;

    private static string? Name(Dictionary<Guid, string> names, Guid id)
        => names.GetValueOrDefault(id);
}
