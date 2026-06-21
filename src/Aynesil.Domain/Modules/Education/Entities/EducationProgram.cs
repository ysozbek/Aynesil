using Aynesil.Domain.Modules.Education.Events;

namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// Program master record. Program type (program_type) is configurable reference data:
/// individual_education, group_education, therapy, camp, online — or any type a tenant adds.
/// A program is a named service offering; students are assigned to programs via StudentProgram.
/// Translations are stored in ProgramTranslation (sidecar per locale).
/// Maps to education.program.
///
/// Audit: created_at, updated_at only (created_by/updated_by absent from DDL — see config).
/// Soft delete: deleted_at.
/// Concurrency: row_version.
/// Unique: (corporation_id, code).
/// </summary>
public class EducationProgram : TenantEntity
{
    /// <summary>Human-friendly per-tenant code, e.g. "ABA-01". Unique within corporation.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>FK to ref.ref_value (ref_type 'program_type'). Configurable.</summary>
    public Guid? ProgramTypeId { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<ProgramTranslation> Translations { get; private set; } = [];
    public ICollection<ProgramService> Services { get; private set; } = [];
    public ICollection<StudentProgram> StudentPrograms { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static EducationProgram Create(
        Guid corporationId,
        string code,
        string name,
        Guid? programTypeId = null,
        string? description = null)
    {
        var program = new EducationProgram
        {
            CorporationId = corporationId,
            Code          = code,
            Name          = name,
            ProgramTypeId = programTypeId,
            Description   = description,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };

        program.AddDomainEvent(new ProgramCreatedEvent(program.Id, corporationId, code, name));

        return program;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Update(
        string code,
        string name,
        Guid? programTypeId,
        string? description)
    {
        Code          = code;
        Name          = name;
        ProgramTypeId = programTypeId;
        Description   = description;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive  = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Sets or replaces the translation for a given locale.
    /// Idempotent — calling twice for the same locale updates the existing record.
    /// </summary>
    public ProgramTranslation SetTranslation(string locale, string name, string? description)
    {
        var existing = Translations.FirstOrDefault(t => t.Locale == locale);

        if (existing is not null)
        {
            existing.Name        = name;
            existing.Description = description;
            return existing;
        }

        var translation = new ProgramTranslation
        {
            ProgramId   = Id,
            Locale      = locale,
            Name        = name,
            Description = description
        };

        Translations.Add(translation);
        return translation;
    }
}
