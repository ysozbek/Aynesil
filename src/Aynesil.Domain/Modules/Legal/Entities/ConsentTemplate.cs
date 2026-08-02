using Aynesil.Domain.Modules.Legal.Events;

namespace Aynesil.Domain.Modules.Legal.Entities;

/// <summary>
/// Maps to legal.consent_template.
/// Versioned, localised consent template for KVKK and other consent types.
/// Only one version per (corporation_id, code) may have IsCurrent = true.
///
/// ConsentTypeId references ref_value(consent_type) — configurable.
/// Examples: data_processing, camera_viewing, media_release, communication_consent, marketing_consent.
///
/// When IsMandatory = true the consent must be obtained before certain actions
/// (e.g. session recording, media release). This flag is stored on the template so
/// every version of that template carries the mandate.
///
/// DDL notes:
///   - Has deleted_at — soft-deleted, never physically removed.
///   - No created_by / updated_by columns in DDL — ignored in EF config.
/// </summary>
public class ConsentTemplate : TenantEntity
{
    public string Code { get; private set; } = string.Empty;

    /// <summary>FK to ref_value(consent_type). Null means no specific type.</summary>
    public Guid? ConsentTypeId { get; private set; }

    public int Version { get; private set; } = 1;
    public bool IsCurrent { get; private set; } = true;

    /// <summary>When true this consent must be obtained to proceed with related business operations.</summary>
    public bool IsMandatory { get; private set; } = false;

    public DateOnly? EffectiveFrom { get; private set; }

    public ICollection<ConsentTemplateTranslation> Translations { get; private set; } = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ConsentTemplate Create(
        Guid corporationId,
        string code,
        Guid? consentTypeId = null,
        bool isMandatory = false,
        DateOnly? effectiveFrom = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Consent template code is required.", nameof(code));

        var template = new ConsentTemplate
        {
            CorporationId = corporationId,
            Code          = code.Trim().ToLowerInvariant(),
            ConsentTypeId = consentTypeId,
            Version       = 1,
            IsCurrent     = true,
            IsMandatory   = isMandatory,
            EffectiveFrom = effectiveFrom
        };

        template.AddDomainEvent(new ConsentTemplateCreatedEvent(
            template.Id, corporationId, template.Code, template.Version));

        return template;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(Guid? consentTypeId, bool isMandatory, DateOnly? effectiveFrom)
    {
        EnsureCurrentAndEditable();
        ConsentTypeId = consentTypeId;
        IsMandatory   = isMandatory;
        EffectiveFrom = effectiveFrom;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void UpsertTranslation(string locale, string title, string body)
    {
        EnsureCurrentAndEditable();
        var existing = Translations.FirstOrDefault(t => t.Locale == locale.ToLowerInvariant());
        if (existing is not null)
            existing.Update(title, body);
        else
            Translations.Add(new ConsentTemplateTranslation(Id, locale, title, body));
    }

    // ── Versioning ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Archives this version and returns a new version entity.
    /// Both entities must be saved in the same transaction by the handler.
    /// </summary>
    public ConsentTemplate NewVersion(DateOnly? effectiveFrom = null)
    {
        EnsureCurrentAndEditable();

        IsCurrent = false;
        UpdatedAt = DateTimeOffset.UtcNow;

        var next = new ConsentTemplate
        {
            CorporationId = CorporationId,
            Code          = Code,
            ConsentTypeId = ConsentTypeId,
            Version       = Version + 1,
            IsCurrent     = true,
            IsMandatory   = IsMandatory,
            EffectiveFrom = effectiveFrom ?? EffectiveFrom
        };

        next.AddDomainEvent(new ConsentTemplateVersionedEvent(
            next.Id, CorporationId, Code, next.Version));

        return next;
    }

    // ── Soft Delete ────────────────────────────────────────────────────────────

    public void Delete(Guid? deletedBy = null)
    {
        EnsureCurrentAndEditable();
        SoftDelete(deletedBy);
    }

    // ── Guards ─────────────────────────────────────────────────────────────────

    private void EnsureCurrentAndEditable()
    {
        if (!IsCurrent)
            throw new InvalidOperationException(
                $"Consent template '{Code}' v{Version} is archived and cannot be modified.");
        if (IsDeleted)
            throw new InvalidOperationException(
                $"Consent template '{Code}' v{Version} has been deleted.");
    }
}
