namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.report_schedule.
/// Configures a recurring execution of a report with a cron expression.
/// The background job processor picks up active schedules and creates ReportRun records.
/// </summary>
public class ReportSchedule : TenantEntity
{
    public Guid ReportDefinitionId { get; set; }

    /// <summary>Standard cron expression, e.g. '0 8 * * 1' (every Monday at 08:00).</summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>Report parameter values as JSON matching the definition's params_schema.</summary>
    public string Params { get; set; } = "{}";

    /// <summary>JSON array of email addresses or user IDs to deliver the output to.</summary>
    public string Recipients { get; set; } = "[]";

    public bool IsActive { get; set; } = true;

    public ReportDefinition? ReportDefinition { get; set; }
}
