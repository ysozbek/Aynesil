namespace Aynesil.Domain.Common;

/// <summary>
/// Base class for entities that are scoped to a corporation AND optionally a specific campus (branch).
/// Campus is not an isolation boundary — it is an authorization sub-scope within a corporation.
/// CampusId nullable: NULL means corporation-wide record; set means campus-scoped record.
/// </summary>
public abstract class TenantBranchEntity : TenantEntity
{
    /// <summary>
    /// Foreign key to core.campus.id.
    /// NULL = record applies to the entire corporation.
    /// Set = record is scoped to a specific campus.
    /// </summary>
    public Guid? CampusId { get; set; }
}
