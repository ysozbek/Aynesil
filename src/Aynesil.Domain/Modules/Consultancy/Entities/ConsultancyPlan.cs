using Aynesil.Domain.Modules.Consultancy.Events;

namespace Aynesil.Domain.Modules.Consultancy.Entities;

/// <summary>
/// Maps to consultancy.consultancy_plan.
/// A structured consultancy engagement linking an institution to a period and scope of work.
/// ConsultancyTypeId references ref_value(consultancy_type) — configurable, never hardcoded.
/// Status workflow: draft → active → completed | cancelled.
/// DB has no deleted_at column — lifecycle is managed by status transitions only.
/// DB columns created_by / updated_by / deleted_at do not exist — ignored in EF config.
/// </summary>
public class ConsultancyPlan : TenantEntity
{
    private static readonly string[] ValidStatuses = ["draft", "active", "completed", "cancelled"];

    public Guid InstitutionId { get; private set; }

    /// <summary>FK to ref_value(consultancy_type). Examples: observation, training, assessment, follow_up.</summary>
    public Guid? ConsultancyTypeId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public DateOnly? PeriodStart { get; private set; }
    public DateOnly? PeriodEnd { get; private set; }
    public string? Scope { get; private set; }

    /// <summary>Lead educator responsible for delivering the consultancy.</summary>
    public Guid? LeadEducatorId { get; private set; }

    /// <summary>'draft' | 'active' | 'completed' | 'cancelled'</summary>
    public string Status { get; private set; } = "draft";

    public Institution Institution { get; private set; } = null!;
    public ICollection<SchoolVisit> Visits { get; private set; } = [];
    public ICollection<ConsultancyReport> Reports { get; private set; } = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ConsultancyPlan Create(
        Guid corporationId,
        Guid institutionId,
        string name,
        Guid? consultancyTypeId = null,
        DateOnly? periodStart = null,
        DateOnly? periodEnd = null,
        string? scope = null,
        Guid? leadEducatorId = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Consultancy plan name is required.", nameof(name));
        if (periodStart.HasValue && periodEnd.HasValue && periodEnd < periodStart)
            throw new ArgumentException("Period end cannot be before period start.");

        var plan = new ConsultancyPlan
        {
            CorporationId     = corporationId,
            InstitutionId     = institutionId,
            ConsultancyTypeId = consultancyTypeId,
            Name              = name.Trim(),
            PeriodStart       = periodStart,
            PeriodEnd         = periodEnd,
            Scope             = scope?.Trim(),
            LeadEducatorId    = leadEducatorId,
            Status            = "draft",
            CreatedBy         = createdBy
        };

        plan.AddDomainEvent(new ConsultancyPlanCreatedEvent(
            plan.Id, corporationId, institutionId, plan.Name));

        return plan;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(
        string name,
        Guid? consultancyTypeId,
        DateOnly? periodStart,
        DateOnly? periodEnd,
        string? scope,
        Guid? leadEducatorId,
        Guid? updatedBy = null)
    {
        EnsureEditable();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Consultancy plan name is required.", nameof(name));
        if (periodStart.HasValue && periodEnd.HasValue && periodEnd < periodStart)
            throw new ArgumentException("Period end cannot be before period start.");

        Name              = name.Trim();
        ConsultancyTypeId = consultancyTypeId;
        PeriodStart       = periodStart;
        PeriodEnd         = periodEnd;
        Scope             = scope?.Trim();
        LeadEducatorId    = leadEducatorId;
        UpdatedAt         = DateTimeOffset.UtcNow;
        UpdatedBy         = updatedBy;
    }

    // ── Workflow ───────────────────────────────────────────────────────────────

    public void Activate(Guid? updatedBy = null)
    {
        if (Status != "draft")
            throw new InvalidOperationException(
                $"Only a draft plan can be activated. Current status: '{Status}'.");

        var prev = Status;
        Status    = "active";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new ConsultancyPlanStatusChangedEvent(
            Id, CorporationId, prev, Status));
    }

    public void Complete(Guid? updatedBy = null)
    {
        if (Status != "active")
            throw new InvalidOperationException(
                $"Only an active plan can be completed. Current status: '{Status}'.");

        var prev = Status;
        Status    = "completed";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new ConsultancyPlanStatusChangedEvent(
            Id, CorporationId, prev, Status));
    }

    public void Cancel(Guid? updatedBy = null)
    {
        if (Status is "completed" or "cancelled")
            throw new InvalidOperationException(
                $"A '{Status}' plan cannot be cancelled.");

        var prev = Status;
        Status    = "cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new ConsultancyPlanStatusChangedEvent(
            Id, CorporationId, prev, Status));
    }

    // ── Guards ─────────────────────────────────────────────────────────────────

    private void EnsureEditable()
    {
        if (Status is "completed" or "cancelled")
            throw new InvalidOperationException(
                $"A '{Status}' consultancy plan cannot be modified.");
    }
}
