using Aynesil.Domain.Modules.Consultancy.Events;

namespace Aynesil.Domain.Modules.Consultancy.Entities;

/// <summary>
/// Maps to consultancy.consultancy_report.
/// A formal report for a consultancy plan or visit, optionally linked to an uploaded document.
/// Reports are append-only (immutable after creation) — no update operations exist.
/// DB schema has only created_at — this entity extends BaseEntity directly.
/// AuthoredBy is a business field distinct from the audit CreatedBy pattern.
/// </summary>
public class ConsultancyReport : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid? ConsultancyPlanId { get; private set; }
    public Guid? SchoolVisitId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Summary { get; private set; }

    /// <summary>FK to core.file_object — optional attached report document.</summary>
    public Guid? FileId { get; private set; }

    /// <summary>The educator or consultant who authored this report.</summary>
    public Guid? AuthoredBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public ConsultancyPlan? Plan { get; private set; }
    public SchoolVisit? Visit { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ConsultancyReport Create(
        Guid corporationId,
        string title,
        Guid? consultancyPlanId = null,
        Guid? schoolVisitId = null,
        string? summary = null,
        Guid? fileId = null,
        Guid? authoredBy = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Report title is required.", nameof(title));
        if (consultancyPlanId == null && schoolVisitId == null)
            throw new ArgumentException(
                "A consultancy report must be linked to a plan, a visit, or both.");
        if (string.IsNullOrWhiteSpace(summary) && fileId == null)
            throw new ArgumentException(
                "A consultancy report must include at least a summary or an attached file.");

        var report = new ConsultancyReport
        {
            CorporationId      = corporationId,
            ConsultancyPlanId  = consultancyPlanId,
            SchoolVisitId      = schoolVisitId,
            Title              = title.Trim(),
            Summary            = summary?.Trim(),
            FileId             = fileId,
            AuthoredBy         = authoredBy,
            CreatedAt          = DateTimeOffset.UtcNow
        };

        report.AddDomainEvent(new ConsultancyReportCreatedEvent(
            report.Id, corporationId, consultancyPlanId, schoolVisitId));

        return report;
    }
}
