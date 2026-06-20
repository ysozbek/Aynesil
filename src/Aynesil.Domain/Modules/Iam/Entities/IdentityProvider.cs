namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.identity_provider.
/// External authentication provider configuration (OIDC, SAML, OAuth2, LDAP, local).
/// CorporationId nullable: NULL = platform-wide provider; set = tenant-specific provider.
/// Config contains non-secret OAuth/OIDC metadata (endpoints, client ID).
/// Client secrets are stored via secret manager references, not in this table.
/// </summary>
public class IdentityProvider : BaseEntity
{
    /// <summary>NULL = platform-wide; set = tenant-specific IdP.</summary>
    public Guid? CorporationId { get; set; }

    public string Code { get; set; } = string.Empty;

    /// <summary>'oidc', 'saml', 'oauth2', 'ldap', 'local'.</summary>
    public string Kind { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Non-secret configuration as JSON (authorization endpoint, client_id, etc.).</summary>
    public string Config { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; set; } = 1;

    public ICollection<UserIdentity> UserIdentities { get; set; } = [];
}
