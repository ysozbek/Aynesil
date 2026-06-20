namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.permission.
/// Platform-wide catalog of resource:action permissions. Not tenant-scoped.
/// Permissions are the source of truth for authorization — never authorize by role name.
/// Convention: 'resource:action' e.g. 'student:read', 'session:create', 'report:export'.
/// New permissions are added when new features are built; roles are then assigned the new permissions.
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>Unique stable code: 'student:read', 'session:create'. Never renamed after release.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The resource being protected: 'student', 'session', 'report'.</summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>The action being permitted: 'read', 'create', 'update', 'delete', 'export'.</summary>
    public string Action { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
