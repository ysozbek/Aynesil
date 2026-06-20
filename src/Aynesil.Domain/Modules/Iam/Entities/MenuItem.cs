namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.menu_item.
/// Dynamic, permission-driven navigation tree.
/// CorporationId nullable: NULL = platform default menu; set = tenant-customized menu item.
/// Visibility is controlled by required_permission_id (permission check) and
/// feature_flag (future SaaS licensing seam via menu_item.feature_flag).
/// The recursive tree is built client-side from the flat list using parent_id.
/// </summary>
public class MenuItem : BaseEntity
{
    public Guid? CorporationId { get; set; }

    public Guid? ParentId { get; set; }

    /// <summary>Stable machine code unique within scope, e.g. 'students.list'.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Frontend route path, e.g. '/students'.</summary>
    public string? Route { get; set; }

    /// <summary>Icon identifier (Keenicons class name or custom).</summary>
    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    /// <summary>If set, the menu item is hidden when the user lacks this permission.</summary>
    public Guid? RequiredPermissionId { get; set; }

    /// <summary>Future SaaS licensing seam. Reserved for feature flag evaluation.</summary>
    public string? FeatureFlag { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; set; } = 1;

    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = [];
    public ICollection<MenuItemTranslation> Translations { get; set; } = [];
    public Permission? RequiredPermission { get; set; }
}
