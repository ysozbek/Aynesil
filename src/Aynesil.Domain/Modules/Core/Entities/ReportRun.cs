namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.report_run.
/// Records a single execution of a report — scheduled or on-demand.
/// ResultFileId links to a core.file_object when the output is exported to a file.
/// </summary>
public class ReportRun : TenantEntity
{
    public Guid ReportDefinitionId { get; set; }

    public string Params { get; set; } = "{}";

    /// <summary>'running', 'succeeded', 'failed'.</summary>
    public string Status { get; set; } = "running";

    /// <summary>FK to core.file_object when the report generates a file output.</summary>
    public Guid? ResultFileId { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FinishedAt { get; set; }

    public Guid? RequestedBy { get; set; }

    public ReportDefinition? ReportDefinition { get; set; }
}
