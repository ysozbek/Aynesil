using Aynesil.Domain.Modules.Consultancy.Events;

namespace Aynesil.Domain.Modules.Consultancy.Entities;

/// <summary>
/// Maps to consultancy.consultancy_agreement.
/// A formal agreement (contract, MOU, NDA) between the corporation and an institution,
/// always linked to a consultancy plan.
/// AgreementTypeId references ref_value(agreement_type) — configurable, never hardcoded.
///
/// Status workflow:  draft → sent → signed (terminal)
///                   draft | sent → cancelled (terminal)
///                   signed → expired (terminal)
///
/// IMMUTABILITY RULE: once an agreement reaches 'signed' status it can never be
/// modified or soft-deleted. All update/delete guards check this invariant.
/// </summary>
public class ConsultancyAgreement : TenantEntity
{
    public Guid ConsultancyPlanId { get; private set; }
    public Guid InstitutionId { get; private set; }

    /// <summary>FK to ref_value(agreement_type). Examples: service_agreement, consultancy_contract, nda.</summary>
    public Guid? AgreementTypeId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? SignedDate { get; private set; }

    /// <summary>'draft' | 'sent' | 'signed' | 'expired' | 'cancelled'</summary>
    public string Status { get; private set; } = "draft";

    /// <summary>FK to core.file_object — the signed agreement document.</summary>
    public Guid? FileId { get; private set; }

    /// <summary>Name/role of the person who signed on behalf of the institution.</summary>
    public string? SignedByName { get; private set; }

    public ConsultancyPlan Plan { get; private set; } = null!;

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ConsultancyAgreement Create(
        Guid corporationId,
        Guid consultancyPlanId,
        Guid institutionId,
        string title,
        Guid? agreementTypeId = null,
        string? description = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Agreement title is required.", nameof(title));
        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
            throw new ArgumentException("End date cannot be before start date.");

        var agreement = new ConsultancyAgreement
        {
            CorporationId      = corporationId,
            ConsultancyPlanId  = consultancyPlanId,
            InstitutionId      = institutionId,
            AgreementTypeId    = agreementTypeId,
            Title              = title.Trim(),
            Description        = description?.Trim(),
            StartDate          = startDate,
            EndDate            = endDate,
            Status             = "draft",
            CreatedBy          = createdBy
        };

        agreement.AddDomainEvent(new ConsultancyAgreementCreatedEvent(
            agreement.Id, corporationId, consultancyPlanId, agreement.Title));

        return agreement;
    }

    // ── Mutations (draft only) ─────────────────────────────────────────────────

    public void Update(
        string title,
        Guid? agreementTypeId,
        string? description,
        DateOnly? startDate,
        DateOnly? endDate,
        Guid? updatedBy = null)
    {
        EnsureDraft("update");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Agreement title is required.", nameof(title));
        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
            throw new ArgumentException("End date cannot be before start date.");

        Title           = title.Trim();
        AgreementTypeId = agreementTypeId;
        Description     = description?.Trim();
        StartDate       = startDate;
        EndDate         = endDate;
        UpdatedAt       = DateTimeOffset.UtcNow;
        UpdatedBy       = updatedBy;
    }

    // ── Workflow ───────────────────────────────────────────────────────────────

    /// <summary>Sends the agreement to the institution for review (draft → sent).</summary>
    public void Send(Guid? updatedBy = null)
    {
        EnsureDraft("send");
        var prev = Status;
        Status    = "sent";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(new ConsultancyAgreementStatusChangedEvent(Id, CorporationId, prev, Status));
    }

    /// <summary>Records that the institution has signed the agreement (sent → signed). Immutable after this.</summary>
    public void Sign(DateOnly signedDate, string? signedByName, Guid? fileId, Guid? updatedBy = null)
    {
        if (Status != "sent")
            throw new InvalidOperationException(
                $"Only a sent agreement can be signed. Current status: '{Status}'.");

        var prev      = Status;
        Status        = "signed";
        SignedDate    = signedDate;
        SignedByName  = signedByName?.Trim();
        FileId        = fileId ?? FileId;
        UpdatedAt     = DateTimeOffset.UtcNow;
        UpdatedBy     = updatedBy;

        AddDomainEvent(new ConsultancyAgreementSignedEvent(
            Id, CorporationId, ConsultancyPlanId, signedDate));
        AddDomainEvent(new ConsultancyAgreementStatusChangedEvent(Id, CorporationId, prev, Status));
    }

    /// <summary>Marks a signed agreement as expired.</summary>
    public void MarkExpired(Guid? updatedBy = null)
    {
        if (Status != "signed")
            throw new InvalidOperationException(
                $"Only a signed agreement can be expired. Current status: '{Status}'.");

        var prev  = Status;
        Status    = "expired";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(new ConsultancyAgreementStatusChangedEvent(Id, CorporationId, prev, Status));
    }

    /// <summary>Cancels the agreement. Not allowed once signed.</summary>
    public void Cancel(Guid? updatedBy = null)
    {
        if (Status is "signed" or "expired" or "cancelled")
            throw new InvalidOperationException(
                $"A '{Status}' agreement cannot be cancelled.");

        var prev  = Status;
        Status    = "cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(new ConsultancyAgreementStatusChangedEvent(Id, CorporationId, prev, Status));
    }

    // ── Guards ─────────────────────────────────────────────────────────────────

    public bool IsSigned => Status is "signed" or "expired";

    private void EnsureDraft(string action)
    {
        if (Status != "draft")
            throw new InvalidOperationException(
                $"Cannot {action} a '{Status}' agreement. Only draft agreements can be modified.");
    }
}
