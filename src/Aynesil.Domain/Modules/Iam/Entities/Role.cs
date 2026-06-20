using Aynesil.Domain.Modules.Iam.Events;

namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.role.
/// CorporationId nullable: NULL = system role template; set = tenant-specific role.
/// System roles (is_system=true) are cloned per tenant at onboarding and can be
/// customized by the tenant's admins (adding/removing permissions, creating new roles).
/// Roles are configurable — never hard-code role names in authorization logic.
/// </summary>
public class Role : BaseEntity
{
    /// <summary>NULL = system template; set = cloned tenant role.</summary>
    public Guid? CorporationId { get; set; }

    /// <summary>Stable code within scope, e.g. 'admin', 'educator', 'therapist'.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Platform-owned template roles cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
    public int RowVersion { get; set; } = 1;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new tenant role. Pass corporationId=null only for platform-level system templates.
    /// </summary>
    public static Role Create(
        Guid? corporationId,
        string code,
        string name,
        string? description = null,
        bool isSystem = false)
    {
        var role = new Role
        {
            CorporationId = corporationId,
            Code = code.ToLowerInvariant(),
            Name = name,
            Description = description,
            IsSystem = isSystem
        };
        role.AddDomainEvent(new RoleCreatedEvent(role.Id, corporationId, role.Code));
        return role;
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>Updates the display name and description.</summary>
    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Soft-deletes the role. System roles cannot be deleted.
    /// All user_role grants referencing this role will be orphaned — revoke them before deleting.
    /// </summary>
    public void SoftDelete()
    {
        if (IsSystem)
            throw new InvalidOperationException("System roles cannot be deleted.");

        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
