using Aynesil.Domain.Modules.Legal.Events;

namespace Aynesil.Domain.Modules.Legal.Entities;

/// <summary>
/// Maps to legal.student_consent.
/// KVKK consent ledger: each row represents a consent grant or withdrawal for a student/guardian.
/// Records are NEVER physically deleted — state transitions are the only lifecycle mechanism.
/// The ledger is append-friendly: each new consent (or re-grant after withdrawal) is a new row.
///
/// State machine: granted ⟷ withdrawn (expired is set by automated processes)
/// ConsentTypeId references ref_value(consent_type): data_processing, camera_viewing, etc.
/// TemplateId + TemplateVersion record which version of the consent text the guardian agreed to
/// (KVKK compliance requirement — evidence of exact wording shown).
/// EvidenceFileId: optional signed/scanned consent form stored in core.file_object.
///
/// DDL notes:
///   - No deleted_at — physical delete is forbidden. State transitions manage lifecycle.
///   - Has created_by — mapped.
///   - No updated_by — ignored in EF config.
/// </summary>
public class StudentConsent : TenantEntity
{
    public Guid StudentId { get; private set; }
    public Guid? GuardianId { get; private set; }

    /// <summary>FK to legal.consent_template. Records the template version presented to the guardian.</summary>
    public Guid? TemplateId { get; private set; }

    public int? TemplateVersion { get; private set; }

    /// <summary>FK to ref_value(consent_type). Examples: data_processing, camera_viewing, media_release.</summary>
    public Guid? ConsentTypeId { get; private set; }

    /// <summary>'granted' | 'withdrawn' | 'expired'</summary>
    public string State { get; private set; } = "granted";

    public DateTimeOffset? GrantedAt { get; private set; }
    public DateTimeOffset? WithdrawnAt { get; private set; }

    /// <summary>Optional expiry date. NULL = never expires automatically.</summary>
    public DateOnly? ValidUntil { get; private set; }

    /// <summary>FK to core.file_object — optional signed or scanned consent evidence.</summary>
    public Guid? EvidenceFileId { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static StudentConsent Grant(
        Guid corporationId,
        Guid studentId,
        Guid? guardianId = null,
        Guid? templateId = null,
        int? templateVersion = null,
        Guid? consentTypeId = null,
        DateOnly? validUntil = null,
        Guid? evidenceFileId = null,
        Guid? createdBy = null)
    {
        var consent = new StudentConsent
        {
            CorporationId   = corporationId,
            StudentId       = studentId,
            GuardianId      = guardianId,
            TemplateId      = templateId,
            TemplateVersion = templateVersion,
            ConsentTypeId   = consentTypeId,
            State           = "granted",
            GrantedAt       = DateTimeOffset.UtcNow,
            ValidUntil      = validUntil,
            EvidenceFileId  = evidenceFileId,
            CreatedBy       = createdBy
        };

        consent.AddDomainEvent(new ConsentGrantedEvent(consent.Id, corporationId, studentId, consentTypeId));
        return consent;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Withdraws a previously granted consent. Immutable once withdrawn — no re-opening.
    /// To re-grant, create a new StudentConsent row via Grant().
    /// </summary>
    public void Withdraw(Guid? updatedBy = null)
    {
        if (State != "granted")
            throw new InvalidOperationException(
                $"Only a granted consent can be withdrawn. Current state: '{State}'.");

        State       = "withdrawn";
        WithdrawnAt = DateTimeOffset.UtcNow;
        UpdatedAt   = DateTimeOffset.UtcNow;

        AddDomainEvent(new ConsentWithdrawnEvent(Id, CorporationId, StudentId, ConsentTypeId));
    }

    /// <summary>Attaches or replaces the evidence file reference. Only allowed while granted.</summary>
    public void AttachEvidence(Guid evidenceFileId, Guid? updatedBy = null)
    {
        if (State != "granted")
            throw new InvalidOperationException("Evidence can only be attached to a granted consent.");

        EvidenceFileId = evidenceFileId;
        UpdatedAt      = DateTimeOffset.UtcNow;
    }
}
