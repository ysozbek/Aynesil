namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.setting_definition.
/// Defines a configurable platform setting: its key, data type, default value,
/// and which scope levels are allowed (system / corporation / campus / user).
/// Settings are resolved most-specific-wins: user > campus > corporation > system default.
/// Adding a new setting is a seed INSERT, not a schema change.
/// </summary>
public class SettingDefinition : BaseEntity
{
    /// <summary>Unique setting key, e.g. 'scheduling.session.default_duration'.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>'string', 'integer', 'decimal', 'boolean', 'json', 'date'.</summary>
    public string DataType { get; set; } = "string";

    /// <summary>System default value as JSON.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Allowed scope levels as array: '{corporation}', '{corporation,campus}', etc.</summary>
    public string[] ScopeLevels { get; set; } = ["corporation"];

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<SettingValue> Values { get; set; } = [];
}
