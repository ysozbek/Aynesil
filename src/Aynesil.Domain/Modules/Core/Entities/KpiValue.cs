namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.kpi_value.
/// Computed KPI snapshot for a subject (educator, campus, corporation, program) over a period.
/// Written by the KPI computation job; never mutated after creation.
/// Unique per (kpi_id, subject_type, subject_id, period).
/// </summary>
public class KpiValue : TenantEntity
{
    public Guid KpiId { get; set; }

    /// <summary>'educator', 'campus', 'corporation', 'program'.</summary>
    public string SubjectType { get; set; } = string.Empty;

    public Guid? SubjectId { get; set; }

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public decimal? NumericValue { get; set; }

    /// <summary>Supplementary breakdown data as JSON.</summary>
    public string Detail { get; set; } = "{}";

    public DateTimeOffset ComputedAt { get; set; } = DateTimeOffset.UtcNow;

    public KpiDefinition? KpiDefinition { get; set; }
}
