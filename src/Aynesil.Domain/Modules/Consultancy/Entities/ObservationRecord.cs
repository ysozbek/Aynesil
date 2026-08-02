using Aynesil.Domain.Modules.Consultancy.Events;

namespace Aynesil.Domain.Modules.Consultancy.Entities;

/// <summary>
/// Maps to consultancy.observation_record.
/// A single observation recorded during a school visit, with optional recommendations.
/// ObservationTypeId references ref_value(observation_type) — configurable, never hardcoded.
/// DB schema has only created_at and created_by — this entity extends BaseEntity directly.
/// No updated_at, row_version, or deleted_at columns exist in the DB.
/// </summary>
public class ObservationRecord : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid SchoolVisitId { get; private set; }

    /// <summary>FK to ref_value(observation_type). Examples: classroom, individual, group, teacher, environment.</summary>
    public Guid? ObservationTypeId { get; private set; }

    /// <summary>The class, teacher, child, or context being observed (free text).</summary>
    public string? Subject { get; private set; }

    /// <summary>Full observation narrative. Required.</summary>
    public string Observation { get; private set; } = string.Empty;

    /// <summary>Actionable recommendations arising from the observation.</summary>
    public string? Recommendations { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>References iam.user_account.id of the observer.</summary>
    public Guid? CreatedBy { get; private set; }

    public SchoolVisit Visit { get; private set; } = null!;

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ObservationRecord Record(
        Guid corporationId,
        Guid schoolVisitId,
        string observation,
        Guid? observationTypeId = null,
        string? subject = null,
        string? recommendations = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(observation))
            throw new ArgumentException("Observation text is required.", nameof(observation));

        var record = new ObservationRecord
        {
            CorporationId    = corporationId,
            SchoolVisitId    = schoolVisitId,
            ObservationTypeId = observationTypeId,
            Subject          = subject?.Trim(),
            Observation      = observation.Trim(),
            Recommendations  = recommendations?.Trim(),
            CreatedAt        = DateTimeOffset.UtcNow,
            CreatedBy        = createdBy
        };

        record.AddDomainEvent(new ObservationRecordedEvent(
            record.Id, corporationId, schoolVisitId));

        return record;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(
        string observation,
        Guid? observationTypeId,
        string? subject,
        string? recommendations)
    {
        if (string.IsNullOrWhiteSpace(observation))
            throw new ArgumentException("Observation text is required.", nameof(observation));

        Observation       = observation.Trim();
        ObservationTypeId = observationTypeId;
        Subject           = subject?.Trim();
        Recommendations   = recommendations?.Trim();
    }
}
