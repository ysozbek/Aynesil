namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.user_role.
/// Grants a role to a user, optionally scoped to a specific campus.
/// CampusId null = corporation-wide grant.
/// CampusId set = campus-scoped grant (user has this role only at that campus).
/// ValidFrom/ValidTo support time-bounded role grants (e.g. temporary admin access).
/// </summary>
public class UserRole : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    /// <summary>Null = corporation-wide; set = campus-scoped grant.</summary>
    public Guid? CampusId { get; set; }

    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }

    public UserAccount? User { get; set; }
    public Role? Role { get; set; }
}
