using Aynesil.Domain.Modules.Iam.Events;

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
    public string Status { get; set; } = "invited";

    /// <summary>User's preferred display locale. FK to ref.locale.</summary>
    public string? PreferredLocale { get; set; }

    /// <summary>Default campus for this user. FK to core.campus.</summary>
    public Guid? PrimaryCampusId { get; set; }

    public bool MfaEnabled { get; set; }

    /// <summary>TOTP secret (encrypted at rest). Null if MFA not configured.</summary>
    public string? MfaSecret { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// true: hesabı yönetici açtı (parolayı yönetici belirledi/geçici parola); sahibi değiştirene kadar yalnızca
    /// parola değiştirme ve çıkış uçları kullanılabilir (JWT "mcp" claim'i). <see cref="ChangePassword"/> false yapar.
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// Bu andan önce verilmiş erişim jetonları (JWT <c>iat</c>) geçersizdir (V63). Şifre değişimi/sıfırlama, askıya alma,
    /// devre dışı bırakma, silme ve rol/izin değişikliğinde güncellenir; API her istekte denetler. NULL = iptal yok.
    /// </summary>
    public DateTimeOffset? SessionsRevokedAt { get; set; }

    public ICollection<UserRole> Roles { get; set; } = [];
    public ICollection<AuthSession> Sessions { get; set; } = [];
    public ICollection<UserIdentity> ExternalIdentities { get; set; } = [];

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new user account. Status defaults to 'invited' until email is verified.
    /// An 'active' status can be set immediately when the admin creates the account directly (no invite flow).
    /// </summary>
    public static UserAccount Create(
        Guid corporationId,
        string username,
        string fullName,
        string? email = null,
        string? phone = null,
        string? passwordHash = null,
        string? preferredLocale = null,
        Guid? primaryCampusId = null,
        Guid? createdBy = null,
        bool activateImmediately = false,
        bool mustChangePassword = false)
    {
        var user = new UserAccount
        {
            CorporationId = corporationId,
            Username = username.ToLowerInvariant(),
            FullName = fullName,
            Email = email?.ToLowerInvariant(),
            Phone = phone,
            PasswordHash = passwordHash,
            Status = activateImmediately ? "active" : "invited",
            PreferredLocale = preferredLocale,
            PrimaryCampusId = primaryCampusId,
            MustChangePassword = mustChangePassword,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
        user.AddDomainEvent(new UserCreatedEvent(user.Id, corporationId, user.Username));
        return user;
    }

    // ── Status transitions ────────────────────────────────────────────────────

    /// <summary>Activates an invited or previously suspended account.</summary>
    public void Activate(Guid? updatedBy = null)
    {
        var previous = Status;
        Status = "active";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
        if (previous != Status)
            AddDomainEvent(new UserStatusChangedEvent(Id, CorporationId, previous, Status));
    }

    /// <summary>Temporarily suspends the account. The user cannot log in while suspended.</summary>
    public void Suspend(Guid? updatedBy = null)
    {
        var previous = Status;
        Status = "suspended";
        RevokeSessions();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
        if (previous != Status)
            AddDomainEvent(new UserStatusChangedEvent(Id, CorporationId, previous, Status));
    }

    /// <summary>Permanently disables the account. Stronger than suspension.</summary>
    public void Disable(Guid? updatedBy = null)
    {
        var previous = Status;
        Status = "disabled";
        RevokeSessions();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
        if (previous != Status)
            AddDomainEvent(new UserStatusChangedEvent(Id, CorporationId, previous, Status));
    }

    // ── Profile & credentials ─────────────────────────────────────────────────

    /// <summary>Updates mutable profile fields. Raise event externally if audit trail required.</summary>
    public void UpdateProfile(
        string fullName,
        string? phone,
        string? email,
        string? preferredLocale,
        Guid? primaryCampusId,
        Guid? updatedBy = null)
    {
        FullName = fullName;
        Phone = phone;
        Email = email?.ToLowerInvariant();
        PreferredLocale = preferredLocale;
        PrimaryCampusId = primaryCampusId;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
    }

    /// <summary>Replaces the password hash and invalidates issued access tokens; refresh tokens are revoked by the caller.</summary>
    public void ChangePassword(string newPasswordHash, Guid? updatedBy = null)
    {
        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        RevokeSessions();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
        AddDomainEvent(new UserPasswordChangedEvent(Id, CorporationId));
    }

    /// <summary>
    /// Yönetici, kullanıcıya yeni bir geçici şifre verir: kullanıcı ilk girişte şifresini değiştirmek zorundadır
    /// (<see cref="MustChangePassword"/> = true). Oturumların iptali çağıranın işidir (token servisi).
    /// </summary>
    public void ResetPasswordByAdmin(string temporaryPasswordHash, Guid? resetBy)
    {
        PasswordHash = temporaryPasswordHash;
        MustChangePassword = true;
        RevokeSessions();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = resetBy ?? UpdatedBy;
        AddDomainEvent(new UserPasswordChangedEvent(Id, CorporationId));
    }

    /// <summary>
    /// Şu ana kadar verilmiş bütün erişim jetonlarını geçersiz kılar (<see cref="SessionsRevokedAt"/>). Yenileme
    /// jetonlarının (iam.auth_session) iptali ve API önbelleğinin temizlenmesi çağıranın işidir.
    /// </summary>
    public void RevokeSessions() => SessionsRevokedAt = DateTimeOffset.UtcNow;

    /// <summary>Updates the last login timestamp. Called by the token service after successful authentication.</summary>
    public void RecordLogin() => LastLoginAt = DateTimeOffset.UtcNow;
}
