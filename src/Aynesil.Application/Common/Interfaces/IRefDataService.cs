namespace Aynesil.Application.Common.Interfaces;

public record RefValueDto(
    Guid Id,
    string Code,
    string Label,
    string? ShortLabel,
    string? ParentCode,
    int SortOrder,
    bool IsDefault,
    bool IsSystem,
    string Metadata);

/// <summary>
/// Tenant-aware reference data lookup service.
/// Returns effective values for the current tenant: shared values merged with tenant overrides
/// (matching the DB view ref.v_effective_ref_value).
/// Results are cached per (corporation, type_code) with a 30-minute TTL.
/// </summary>
public interface IRefDataService
{
    Task<IReadOnlyList<RefValueDto>> GetValuesAsync(
        string typeCode,
        bool activeOnly = true,
        CancellationToken ct = default);

    Task<RefValueDto?> GetDefaultAsync(string typeCode, CancellationToken ct = default);

    Task<RefValueDto?> GetByCodeAsync(string typeCode, string code, CancellationToken ct = default);

    Task<RefValueDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
