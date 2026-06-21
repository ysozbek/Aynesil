using System.Text.Json;

namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// An immutable revision record capturing the full plan state at the time of revision.
/// Created whenever EducationPlan.CreateRevision() is called.
/// The snapshot (jsonb) holds the serialized plan + goals at the revision timestamp.
/// Maps to education.education_plan_revision.
/// </summary>
public class EducationPlanRevision : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid EducationPlanId { get; private set; }

    public int FromVersion { get; private set; }
    public int ToVersion { get; private set; }

    public string? ChangeSummary { get; private set; }

    /// <summary>Full JSON snapshot of the plan (and linked goals) at revision time.</summary>
    public JsonDocument? Snapshot { get; private set; }

    /// <summary>FK to educators.educator — the educator who initiated the revision.</summary>
    public Guid? RevisedBy { get; private set; }

    public DateTimeOffset RevisedAt { get; private set; } = DateTimeOffset.UtcNow;

    // ── Constructor (called by EducationPlan.CreateRevision) ──────────────────

    internal EducationPlanRevision(
        Guid corporationId,
        Guid educationPlanId,
        int fromVersion,
        int toVersion,
        string? changeSummary,
        object? snapshot,
        Guid? revisedBy)
    {
        CorporationId   = corporationId;
        EducationPlanId = educationPlanId;
        FromVersion     = fromVersion;
        ToVersion       = toVersion;
        ChangeSummary   = changeSummary;
        RevisedBy       = revisedBy;
        RevisedAt       = DateTimeOffset.UtcNow;

        if (snapshot is not null)
        {
            var json = JsonSerializer.Serialize(snapshot);
            Snapshot = JsonDocument.Parse(json);
        }
    }

    private EducationPlanRevision() { }
}
