namespace Aynesil.Domain.Common;

/// <summary>
/// Extends AuditableEntity with soft-delete support.
/// Records are never physically deleted; DeletedAt is set instead.
/// EF Core global query filters exclude rows where DeletedAt IS NOT NULL.
/// If a business record references a soft-deleted value the FK constraint
/// still holds — deactivation (is_active = false) is preferred for reference data.
/// </summary>
public abstract class SoftDeleteEntity : AuditableEntity
{
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Computed from DeletedAt — no dedicated DB column required.</summary>
    public bool IsDeleted => DeletedAt.HasValue;

    /// <summary>
    /// Marks the entity as soft-deleted.
    /// Callers should also set UpdatedAt/UpdatedBy before persisting.
    /// </summary>
    public void SoftDelete(Guid? deletedBy = null)
    {
        DeletedAt = DateTimeOffset.UtcNow;
        if (deletedBy.HasValue)
            UpdatedBy = deletedBy;
    }

    /// <summary>Restores a soft-deleted entity.</summary>
    public void Restore()
    {
        DeletedAt = null;
    }
}
