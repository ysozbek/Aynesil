namespace Aynesil.Domain.Common;

/// <summary>
/// Base class for entities that are strictly scoped to a single corporation (tenant).
/// CorporationId is NOT NULL — every row belongs to exactly one tenant.
/// PostgreSQL RLS enforces tenant isolation at the DB level using app.current_corporation_id GUC.
/// The EF Core global query filter provides a secondary application-level guard.
/// </summary>
public abstract class TenantEntity : SoftDeleteEntity
{
    /// <summary>
    /// Foreign key to core.corporation.id. Set by the application from tenant context;
    /// validated by RLS with-check constraint to prevent cross-tenant writes.
    /// </summary>
    public Guid CorporationId { get; set; }
}
