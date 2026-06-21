namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// A named collection of reusable goal templates.
/// CorporationId = NULL means platform-provided (shared across all tenants).
/// CorporationId set means tenant-owned library.
/// Maps to education.goal_library.
///
/// Audit: created_at, updated_at, row_version.
/// No soft delete on this table (libraries are deactivated by removing templates, not deleted).
/// Absent from DDL (ignored in config): created_by, updated_by.
/// </summary>
public class GoalLibrary : AuditableEntity
{
    /// <summary>NULL = platform-provided library visible to all tenants.</summary>
    public Guid? CorporationId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<GoalTemplate> Templates { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static GoalLibrary Create(
        Guid? corporationId,
        string name,
        string? description = null)
    {
        return new GoalLibrary
        {
            CorporationId = corporationId,
            Name          = name,
            Description   = description,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Update(string name, string? description)
    {
        Name        = name;
        Description = description;
        UpdatedAt   = DateTimeOffset.UtcNow;
    }
}
