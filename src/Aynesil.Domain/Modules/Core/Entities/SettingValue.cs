namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.setting_value.
/// A concrete value for a setting at a specific scope level.
/// Resolution order (most-specific-wins): user > campus > corporation > system default.
/// ScopeId: campus_id or user_id depending on ScopeLevel; null for system/corporation scope.
/// </summary>
public class SettingValue : BaseEntity
{
    public string SettingKey { get; set; } = string.Empty;

    /// <summary>'system', 'corporation', 'campus', 'user'.</summary>
    public string ScopeLevel { get; set; } = string.Empty;

    public Guid? CorporationId { get; set; }

    /// <summary>campus_id when ScopeLevel='campus'; user_account_id when ScopeLevel='user'.</summary>
    public Guid? ScopeId { get; set; }

    /// <summary>The setting value serialized as JSON.</summary>
    public string Value { get; set; } = "null";

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }

    public SettingDefinition? Definition { get; set; }
}
