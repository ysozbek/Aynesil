namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.user_account.
/// Authentication identity for a user within a corporation.
/// Educators and guardians (Layer 2) link to a UserAccount for portal/app access.
/// password_hash is null when authenticated only via an external IdP (SSO/OIDC/SAML).
/// </summary>
public class UserAccount : TenantEntity
{
    /// <summary>Unique within a corporation. Case-insensitive (citext).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Optional. Case-insensitive unique index per corporation.</summary>
    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>BCrypt hash. Null when authenticated via external IdP only.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>'invited', 'active', 'suspended', 'disabled'.</summary>
    public string Status { get; set; } = "active";

    /// <summary>User's preferred display locale. FK to ref.locale.</summary>
    public string? PreferredLocale { get; set; }

    /// <summary>Default campus for this user. FK to core.campus.</summary>
    public Guid? PrimaryCampusId { get; set; }

    public bool MfaEnabled { get; set; }

    /// <summary>TOTP secret (encrypted at rest). Null if MFA not configured.</summary>
    public string? MfaSecret { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRole> Roles { get; set; } = [];
    public ICollection<AuthSession> Sessions { get; set; } = [];
    public ICollection<UserIdentity> ExternalIdentities { get; set; } = [];
}
