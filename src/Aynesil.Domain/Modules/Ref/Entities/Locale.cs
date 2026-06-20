namespace Aynesil.Domain.Modules.Ref.Entities;

/// <summary>
/// Maps to ref.locale. Text primary key (BCP-47 code: 'tr', 'en', 'en-US').
/// System reference data — managed by platform team, not tenants.
/// Does NOT inherit BaseEntity because its PK is a string, not a UUID.
/// </summary>
public class Locale
{
    /// <summary>BCP-47 locale code (PK). E.g. 'tr', 'en', 'en-US'.</summary>
    public string Code { get; set; } = string.Empty;

    public string EnglishName { get; set; } = string.Empty;

    public string NativeName { get; set; } = string.Empty;

    /// <summary>'ltr' or 'rtl'.</summary>
    public string Direction { get; set; } = "ltr";

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
