using Aynesil.Domain.Modules.Consultancy.Events;

namespace Aynesil.Domain.Modules.Consultancy.Entities;

/// <summary>
/// Maps to consultancy.institution.
/// A partner school or educational institution served by the consultancy program.
/// InstitutionTypeId references ref_value(institution_type) — configurable, never hardcoded.
/// DB columns created_by / updated_by do not exist — ignored in EF config.
/// Supports soft-delete via deleted_at.
/// </summary>
public class Institution : TenantEntity
{
    /// <summary>FK to ref_value(institution_type). Examples: kindergarten, primary_school, high_school.</summary>
    public Guid? InstitutionTypeId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? ContactName { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? ContactEmail { get; private set; }

    public ICollection<ConsultancyPlan> Plans { get; private set; } = [];
    public ICollection<SchoolVisit> Visits { get; private set; } = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static Institution Create(
        Guid corporationId,
        string name,
        Guid? institutionTypeId = null,
        string? city = null,
        string? district = null,
        string? contactName = null,
        string? contactPhone = null,
        string? contactEmail = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Institution name is required.", nameof(name));

        var institution = new Institution
        {
            CorporationId    = corporationId,
            InstitutionTypeId = institutionTypeId,
            Name             = name.Trim(),
            City             = city?.Trim(),
            District         = district?.Trim(),
            ContactName      = contactName?.Trim(),
            ContactPhone     = contactPhone?.Trim(),
            ContactEmail     = contactEmail?.Trim().ToLowerInvariant(),
            CreatedBy        = createdBy
        };

        institution.AddDomainEvent(new InstitutionCreatedEvent(
            institution.Id, corporationId, institution.Name));

        return institution;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(
        string name,
        Guid? institutionTypeId,
        string? city,
        string? district,
        string? contactName,
        string? contactPhone,
        string? contactEmail,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Institution name is required.", nameof(name));

        Name              = name.Trim();
        InstitutionTypeId = institutionTypeId;
        City              = city?.Trim();
        District          = district?.Trim();
        ContactName       = contactName?.Trim();
        ContactPhone      = contactPhone?.Trim();
        ContactEmail      = contactEmail?.Trim().ToLowerInvariant();
        UpdatedAt         = DateTimeOffset.UtcNow;
        UpdatedBy         = updatedBy;
    }
}
