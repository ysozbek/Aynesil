namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.role_permission. Composite PK (role_id, permission_id).
/// Junction table granting a permission to a role.
/// Many-to-many with cascading delete so removing a role removes all its grants.
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
