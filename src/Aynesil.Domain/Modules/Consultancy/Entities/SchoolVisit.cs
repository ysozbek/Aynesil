using Aynesil.Domain.Modules.Consultancy.Events;

namespace Aynesil.Domain.Modules.Consultancy.Entities;

/// <summary>
/// Maps to consultancy.school_visit.
/// A scheduled or completed on-site visit to an institution, optionally linked to a consultancy plan.
/// Status workflow: planned → completed | cancelled.
/// DB schema has only created_at — this entity extends BaseEntity with manual audit fields.
/// No updated_at, row_version, or deleted_at columns exist in the DB.
/// </summary>
public class SchoolVisit : BaseEntity
{
    private static readonly string[] ValidStatuses = ["planned", "completed", "cancelled"];

    public Guid CorporationId { get; private set; }
    public Guid? ConsultancyPlanId { get; private set; }
    public Guid InstitutionId { get; private set; }
    public DateOnly VisitDate { get; private set; }

    /// <summary>Educator conducting the visit.</summary>
    public Guid? VisitorId { get; private set; }

    public string? Purpose { get; private set; }

    /// <summary>'planned' | 'completed' | 'cancelled'</summary>
    public string Status { get; private set; } = "planned";

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Institution Institution { get; private set; } = null!;
    public ConsultancyPlan? Plan { get; private set; }
    public ICollection<ObservationRecord> Observations { get; private set; } = [];
    public ICollection<ConsultancyReport> Reports { get; private set; } = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static SchoolVisit Schedule(
        Guid corporationId,
        Guid institutionId,
        DateOnly visitDate,
        Guid? consultancyPlanId = null,
        Guid? visitorId = null,
        string? purpose = null)
    {
        var visit = new SchoolVisit
        {
            CorporationId      = corporationId,
            InstitutionId      = institutionId,
            ConsultancyPlanId  = consultancyPlanId,
            VisitDate          = visitDate,
            VisitorId          = visitorId,
            Purpose            = purpose?.Trim(),
            Status             = "planned",
            CreatedAt          = DateTimeOffset.UtcNow
        };

        visit.AddDomainEvent(new SchoolVisitScheduledEvent(
            visit.Id, corporationId, institutionId, visitDate, visitorId));

        return visit;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(
        DateOnly visitDate,
        Guid? visitorId,
        string? purpose,
        Guid? consultancyPlanId)
    {
        EnsurePlanned("update");
        VisitDate         = visitDate;
        VisitorId         = visitorId;
        Purpose           = purpose?.Trim();
        ConsultancyPlanId = consultancyPlanId;
    }

    // ── Workflow ───────────────────────────────────────────────────────────────

    public void Complete()
    {
        EnsurePlanned("complete");
        Status = "completed";
        AddDomainEvent(new SchoolVisitCompletedEvent(
            Id, CorporationId, InstitutionId, VisitDate));
    }

    public void Cancel()
    {
        EnsurePlanned("cancel");
        Status = "cancelled";
        AddDomainEvent(new SchoolVisitCancelledEvent(
            Id, CorporationId, InstitutionId, VisitDate));
    }

    // ── Guards ─────────────────────────────────────────────────────────────────

    private void EnsurePlanned(string action)
    {
        if (Status != "planned")
            throw new InvalidOperationException(
                $"Cannot {action} a '{Status}' visit. Only 'planned' visits can be modified.");
    }
}
