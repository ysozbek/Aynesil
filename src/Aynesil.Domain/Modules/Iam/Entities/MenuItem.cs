using Aynesil.Domain.Modules.Iam.Events;

namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.menu_item.
/// Dynamic, permission-driven navigation tree.
/// CorporationId nullable: NULL = platform default menu; set = tenant-customized menu item.
/// Visibility is controlled by required_permission_id (permission check) and
/// feature_flag (future SaaS licensing seam via menu_item.feature_flag).
/// The recursive tree is built from the flat list using parent_id.
/// Platform default items (CorporationId == null) cannot be deleted.
/// </summary>
public class MenuItem : BaseEntity
{
    /// <summary>NULL = platform default; set = tenant-scoped custom item.</summary>
    public Guid? CorporationId { get; private set; }

    public Guid? ParentId { get; private set; }

    /// <summary>Stable machine code unique within scope, e.g. 'students.list'.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Frontend route path, e.g. '/students'.</summary>
    public string? Route { get; private set; }

    /// <summary>Icon identifier (Keenicons class name or custom).</summary>
    public string? Icon { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>If set, the menu item is hidden when the user lacks this permission.</summary>
    public Guid? RequiredPermissionId { get; private set; }

    /// <summary>Future SaaS licensing seam. Reserved for feature flag evaluation.</summary>
    public string? FeatureFlag { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; set; } = 1;

    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = [];
    public ICollection<MenuItemTranslation> Translations { get; set; } = [];
    public Permission? RequiredPermission { get; set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new menu item.
    /// Pass <paramref name="corporationId"/> = null only for platform-level default items.
    /// Code is normalised to lowercase with spaces replaced by hyphens.
    /// </summary>
    public static MenuItem Create(
        Guid? corporationId,
        Guid? parentId,
        string code,
        string? route,
        string? icon,
        int sortOrder = 0,
        Guid? requiredPermissionId = null,
        string? featureFlag = null)
    {
        var item = new MenuItem
        {
            CorporationId = corporationId,
            ParentId = parentId,
            Code = code.Trim().ToLowerInvariant().Replace(' ', '-'),
            Route = route,
            Icon = icon,
            SortOrder = sortOrder,
            RequiredPermissionId = requiredPermissionId,
            FeatureFlag = featureFlag,
            IsActive = true
        };

        item.AddDomainEvent(new MenuItemCreatedEvent(item.Id, corporationId, item.Code));
        return item;
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>Updates the structural and visual properties of the item. Code is immutable.</summary>
    public void Update(
        Guid? parentId,
        string? route,
        string? icon,
        int sortOrder,
        Guid? requiredPermissionId,
        string? featureFlag)
    {
        ParentId = parentId;
        Route = route;
        Icon = icon;
        SortOrder = sortOrder;
        RequiredPermissionId = requiredPermissionId;
        FeatureFlag = featureFlag;
        UpdatedAt = DateTimeOffset.UtcNow;
        RowVersion++;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Guards against deleting a platform default item (CorporationId == null).
    /// Call before any physical delete operation.
    /// </summary>
    public void EnsureCanBeDeleted()
    {
        if (CorporationId is null)
            throw new InvalidOperationException("Platform default menu items cannot be deleted.");
    }
}
