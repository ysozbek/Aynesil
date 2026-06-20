namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.report_definition.
/// Describes a report: its data source, column definitions, and parameter schema.
/// CorporationId nullable: NULL = platform-provided report; set = tenant-custom report.
/// The spec field contains a JSON structure that the reporting engine interprets.
/// </summary>
public class ReportDefinition : BaseEntity
{
    public Guid? CorporationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>FK to ref_value(report_category).</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Report specification (query/dataset/column definitions) as JSON.</summary>
    public string Spec { get; set; } = "{}";

    /// <summary>JSON-Schema describing the required report parameters.</summary>
    public string? ParamsSchema { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; set; } = 1;

    public ICollection<ReportSchedule> Schedules { get; set; } = [];
    public ICollection<ReportRun> Runs { get; set; } = [];
}
