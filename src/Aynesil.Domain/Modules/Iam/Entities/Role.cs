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
}
