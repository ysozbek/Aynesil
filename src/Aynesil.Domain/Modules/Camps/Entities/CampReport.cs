namespace Aynesil.Domain.Modules.Camps.Entities;

/// <summary>
/// Maps to camps.camp_report.
/// An end-of-camp summary report for one enrolled student.
/// May reference an uploaded file via FileId → core.file_object.
/// The DB schema has no update or delete columns — reports are immutable append-only records.
/// </summary>
public class CampReport : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid CampEnrollmentId { get; private set; }
    public string? Summary { get; private set; }

    /// <summary>FK to core.file_object — optional attached report document.</summary>
    public Guid? FileId { get; private set; }

    public Guid? AuthoredBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public CampEnrollment Enrollment { get; private set; } = null!;

    // ── Factory ────────────────────────────────────────────────────────────────

    public static CampReport Create(
        Guid corporationId,
        Guid campEnrollmentId,
        string? summary,
        Guid? fileId = null,
        Guid? authoredBy = null)
    {
        if (string.IsNullOrWhiteSpace(summary) && fileId == null)
            throw new ArgumentException(
                "A camp report must include at least a summary text or an attached file.");

        return new CampReport
        {
            CorporationId    = corporationId,
            CampEnrollmentId = campEnrollmentId,
            Summary          = summary?.Trim(),
            FileId           = fileId,
            AuthoredBy       = authoredBy,
            CreatedAt        = DateTimeOffset.UtcNow
        };
    }
}
