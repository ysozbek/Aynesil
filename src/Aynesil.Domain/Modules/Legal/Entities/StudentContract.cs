using Aynesil.Domain.Modules.Legal.Events;

namespace Aynesil.Domain.Modules.Legal.Entities;

/// <summary>
/// Maps to legal.student_contract.
/// A signed contract instance between the corporation and the student's guardian.
/// Generated from a contract template and progresses through a status workflow.
///
/// Immutability rule: once a contract reaches 'signed', its signature data
/// (SignedAt, SignedByName, SignatureMethod, SignatureRef, SignedFileId) cannot be changed.
/// Status may still advance: signed → active → expired | terminated.
///
/// Signature methods: 'wet' (physical), 'e_sign' (provider), 'click_wrap' (online acceptance).
/// SignatureRef holds the external e-signature provider's transaction/document ID.
/// SignedFileId points to the archived signed PDF in core.file_object.
///
/// Status workflow: draft → sent → signed → active → expired | terminated
///
/// DDL notes:
///   - has created_by — mapped.
///   - no updated_by — ignored in EF config.
///   - has deleted_at — soft-deleted.
/// </summary>
public class StudentContract : TenantEntity
{
    private static readonly string[] ValidSignatureMethods = ["wet", "e_sign", "click_wrap"];

    public Guid StudentId { get; private set; }
    public Guid? TemplateId { get; private set; }
    public int? TemplateVersion { get; private set; }
    public Guid? GuardianId { get; private set; }

    /// <summary>'draft' | 'sent' | 'signed' | 'active' | 'expired' | 'terminated'</summary>
    public string Status { get; private set; } = "draft";

    public DateTimeOffset? SignedAt { get; private set; }
    public string? SignedByName { get; private set; }

    /// <summary>'wet' | 'e_sign' | 'click_wrap'. Null until signed.</summary>
    public string? SignatureMethod { get; private set; }

    /// <summary>E-signature provider's reference ID (document or transaction). Null for wet signatures.</summary>
    public string? SignatureRef { get; private set; }

    /// <summary>FK to core.file_object — the signed PDF. Null until the signed file is uploaded.</summary>
    public Guid? SignedFileId { get; private set; }

    public DateOnly? StartsOn { get; private set; }
    public DateOnly? EndsOn { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static StudentContract Generate(
        Guid corporationId,
        Guid studentId,
        Guid? templateId = null,
        int? templateVersion = null,
        Guid? guardianId = null,
        DateOnly? startsOn = null,
        DateOnly? endsOn = null,
        Guid? createdBy = null)
    {
        if (startsOn.HasValue && endsOn.HasValue && endsOn < startsOn)
            throw new ArgumentException("Contract end date cannot be before start date.");

        var contract = new StudentContract
        {
            CorporationId   = corporationId,
            StudentId       = studentId,
            TemplateId      = templateId,
            TemplateVersion = templateVersion,
            GuardianId      = guardianId,
            Status          = "draft",
            StartsOn        = startsOn,
            EndsOn          = endsOn,
            CreatedBy       = createdBy
        };

        contract.AddDomainEvent(new StudentContractCreatedEvent(contract.Id, corporationId, studentId));
        return contract;
    }

    // ── Mutations (draft/sent only) ────────────────────────────────────────────

    public void UpdateDetails(
        Guid? guardianId,
        DateOnly? startsOn,
        DateOnly? endsOn,
        Guid? updatedBy = null)
    {
        EnsureEditable();
        if (startsOn.HasValue && endsOn.HasValue && endsOn < startsOn)
            throw new ArgumentException("Contract end date cannot be before start date.");

        GuardianId = guardianId;
        StartsOn   = startsOn;
        EndsOn     = endsOn;
        UpdatedAt  = DateTimeOffset.UtcNow;
    }

    // ── Workflow ───────────────────────────────────────────────────────────────

    /// <summary>draft → sent: contract is dispatched to the guardian for signature.</summary>
    public void Send(Guid? updatedBy = null)
    {
        if (Status != "draft")
            throw new InvalidOperationException(
                $"Only draft contracts can be sent. Current status: '{Status}'.");

        Transition("sent");
    }

    /// <summary>
    /// sent → signed: guardian has signed. Once signed, signature data is immutable.
    /// SignedFileId is optional at this stage — the signed PDF may be uploaded later via Activate().
    /// </summary>
    public void Sign(
        string signedByName,
        string signatureMethod,
        string? signatureRef = null,
        Guid? signedFileId = null,
        Guid? updatedBy = null)
    {
        if (Status != "sent")
            throw new InvalidOperationException(
                $"Only sent contracts can be signed. Current status: '{Status}'.");
        if (string.IsNullOrWhiteSpace(signedByName))
            throw new ArgumentException("Signed-by name is required.", nameof(signedByName));
        if (!ValidSignatureMethods.Contains(signatureMethod))
            throw new ArgumentException(
                $"Invalid signature method '{signatureMethod}'. Allowed: wet, e_sign, click_wrap.");

        SignedAt        = DateTimeOffset.UtcNow;
        SignedByName    = signedByName.Trim();
        SignatureMethod = signatureMethod;
        SignatureRef    = signatureRef;
        SignedFileId    = signedFileId;

        var prev = Status;
        Status    = "signed";
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new StudentContractSignedEvent(Id, CorporationId, StudentId, signatureMethod));
        AddDomainEvent(new StudentContractStatusChangedEvent(Id, CorporationId, StudentId, prev, Status));
    }

    /// <summary>signed → active: countersigned by the corporation; contract is in force.</summary>
    public void Activate(Guid? signedFileId = null, Guid? updatedBy = null)
    {
        if (Status != "signed")
            throw new InvalidOperationException(
                $"Only signed contracts can be activated. Current status: '{Status}'.");

        if (signedFileId.HasValue)
            SignedFileId = signedFileId;

        Transition("active");
    }

    /// <summary>active → expired: contract has reached its natural end.</summary>
    public void Expire(Guid? updatedBy = null)
    {
        if (Status != "active")
            throw new InvalidOperationException(
                $"Only active contracts can be expired. Current status: '{Status}'.");
        Transition("expired");
    }

    /// <summary>Terminate: forcefully ends any non-terminal contract (draft → terminated, etc.).</summary>
    public void Terminate(Guid? updatedBy = null)
    {
        if (Status is "expired" or "terminated")
            throw new InvalidOperationException(
                $"A '{Status}' contract cannot be terminated.");
        Transition("terminated");
    }

    // ── Soft Delete ────────────────────────────────────────────────────────────

    public void Delete(Guid? deletedBy = null)
    {
        if (Status != "draft")
            throw new InvalidOperationException(
                "Only draft contracts can be deleted. Use Terminate() for active contracts.");
        SoftDelete(deletedBy);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void Transition(string newStatus)
    {
        var prev = Status;
        Status    = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new StudentContractStatusChangedEvent(Id, CorporationId, StudentId, prev, newStatus));
    }

    private void EnsureEditable()
    {
        if (Status is "signed" or "active" or "expired" or "terminated")
            throw new InvalidOperationException(
                $"A '{Status}' contract cannot be modified.");
    }
}
