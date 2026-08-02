using Aynesil.Domain.Modules.Legal.Events;

namespace Aynesil.Domain.Modules.Legal.Entities;

/// <summary>
/// Maps to legal.contract_template.
/// Versioned, localised contract template. Only one version per (corporation_id, code)
/// may have IsCurrent = true at any time. Previous versions are archived (IsCurrent = false)
/// and must not be modified — immutability of historical versions is a business rule.
///
/// ContractTypeId references ref_value(contract_type) — configurable, never hardcoded.
/// Examples: enrollment_contract, package_agreement, parent_agreement, consultancy_agreement.
///
/// DDL notes:
///   - Has deleted_at — templates are soft-deleted, never physically removed.
///   - No created_by / updated_by columns in DDL — these are ignored in EF config.
/// </summary>
public class ContractTemplate : TenantEntity
{
    public string Code { get; private set; } = string.Empty;

    /// <summary>FK to ref_value(contract_type). Null means no specific type assigned.</summary>
    public Guid? ContractTypeId { get; private set; }

    public int Version { get; private set; } = 1;

    /// <summary>True for the active version; only this version may be edited or used to generate contracts.</summary>
    public bool IsCurrent { get; private set; } = true;

    public DateOnly? EffectiveFrom { get; private set; }

    public ICollection<ContractTemplateTranslation> Translations { get; private set; } = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ContractTemplate Create(
        Guid corporationId,
        string code,
        Guid? contractTypeId = null,
        DateOnly? effectiveFrom = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Contract template code is required.", nameof(code));

        var template = new ContractTemplate
        {
            CorporationId  = corporationId,
            Code           = code.Trim().ToLowerInvariant(),
            ContractTypeId = contractTypeId,
            Version        = 1,
            IsCurrent      = true,
            EffectiveFrom  = effectiveFrom
        };

        template.AddDomainEvent(new ContractTemplateCreatedEvent(
            template.Id, corporationId, template.Code, template.Version));

        return template;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the current version's metadata (type, effective date).
    /// Archived versions are immutable — this guard ensures that invariant.
    /// </summary>
    public void Update(Guid? contractTypeId, DateOnly? effectiveFrom)
    {
        EnsureCurrentAndEditable();
        ContractTypeId = contractTypeId;
        EffectiveFrom  = effectiveFrom;
        UpdatedAt      = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds or replaces a translation for this template version.
    /// Only the current version accepts new translations.
    /// </summary>
    public void UpsertTranslation(string locale, string title, string body)
    {
        EnsureCurrentAndEditable();
        var existing = Translations.FirstOrDefault(t => t.Locale == locale.ToLowerInvariant());
        if (existing is not null)
            existing.Update(title, body);
        else
            Translations.Add(new ContractTemplateTranslation(Id, locale, title, body));
    }

    // ── Versioning ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Archives this version (sets IsCurrent = false) and creates a new version entity
    /// that the caller must persist. This is a two-entity mutation — both must be saved
    /// in the same transaction.
    /// </summary>
    public ContractTemplate NewVersion(DateOnly? effectiveFrom = null)
    {
        EnsureCurrentAndEditable();

        IsCurrent = false;
        UpdatedAt = DateTimeOffset.UtcNow;

        var next = new ContractTemplate
        {
            CorporationId  = CorporationId,
            Code           = Code,
            ContractTypeId = ContractTypeId,
            Version        = Version + 1,
            IsCurrent      = true,
            EffectiveFrom  = effectiveFrom ?? EffectiveFrom
        };

        next.AddDomainEvent(new ContractTemplateVersionedEvent(
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
                $"Contract template '{Code}' v{Version} is archived and cannot be modified.");
        if (IsDeleted)
            throw new InvalidOperationException(
                $"Contract template '{Code}' v{Version} has been deleted.");
    }
}
