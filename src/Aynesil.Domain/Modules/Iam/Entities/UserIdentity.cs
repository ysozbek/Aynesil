namespace Aynesil.Domain.Modules.Iam.Entities;

/// <summary>
/// Maps to iam.user_identity.
/// Links a UserAccount to an external IdP subject (the external user's unique identifier).
/// Unique per (provider, external_subject) to prevent the same external account from
/// being linked to multiple internal accounts.
/// </summary>
public class UserIdentity : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProviderId { get; set; }

    /// <summary>The external provider's unique subject identifier (sub claim in OIDC).</summary>
    public string ExternalSubject { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserAccount? User { get; set; }
    public IdentityProvider? Provider { get; set; }
}
