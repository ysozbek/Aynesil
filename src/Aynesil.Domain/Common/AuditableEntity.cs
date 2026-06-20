namespace Aynesil.Domain.Common;

/// <summary>
/// Extends BaseEntity with audit fields that are present on most platform entities.
/// The DB-level trigger (core.set_updated_at) also maintains updated_at and row_version;
/// the application layer sets these values as a belt-and-suspenders layer.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>References iam.user_account.id of the actor who created the record.</summary>
    public Guid? CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>References iam.user_account.id of the actor who last modified the record.</summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Optimistic concurrency version. Managed by the DB trigger core.set_updated_at().
    /// Configured as a concurrency token in EF Core so concurrent update conflicts surface
    /// as DbUpdateConcurrencyException.
    /// </summary>
    public int RowVersion { get; set; } = 1;
}
