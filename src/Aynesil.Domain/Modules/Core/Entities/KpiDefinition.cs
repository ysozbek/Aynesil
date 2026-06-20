namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.kpi_definition.
/// Defines a Key Performance Indicator: what it measures, how it's computed, and its unit.
/// CorporationId nullable: NULL = platform-provided KPI; set = tenant-custom KPI.
/// The spec field contains the aggregation/formula definition consumed by the KPI engine.
/// </summary>
public class KpiDefinition : BaseEntity
{
    public Guid? CorporationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>FK to ref_value(kpi_category).</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Measurement unit: '%', 'count', 'hours', 'TRY', etc.</summary>
    public string? Unit { get; set; }

    /// <summary>Formula/aggregation definition as JSON.</summary>
    public string Spec { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; set; } = 1;

    public ICollection<KpiValue> Values { get; set; } = [];
}
