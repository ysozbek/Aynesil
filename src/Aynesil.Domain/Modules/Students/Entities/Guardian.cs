using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Guardian (parent / caregiver) master record.
/// Maps to students.guardian.
///
/// Portal access: when a guardian is granted parent portal access, user_id is set to
/// the corresponding iam.user_account. The per-student visibility toggles live in
/// GuardianPortalAccess; user_id here is the credential anchor.
///
/// A guardian may be linked to multiple students via StudentGuardian.
///
/// Audit: full (created_at, created_by, updated_at, updated_by).
/// Soft delete: deleted_at.
/// </summary>
public class Guardian : TenantEntity
{
    /// <summary>FK to iam.user_account.id — non-null when parent portal access is active.</summary>
    public Guid? UserId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    /// <summary>KVKK Art.6 sensitive data.</summary>
    public string? NationalId { get; private set; }

    /// <summary>citext in DB — case-insensitive comparison.</summary>
    public string? Email { get; private set; }

    public string? Phone { get; private set; }
    public string? Occupation { get; private set; }
    public string? AddressLine { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<StudentGuardian> Students { get; private set; } = [];
    public ICollection<GuardianPortalAccess> PortalAccesses { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Guardian Create(
        Guid corporationId,
        string firstName,
        string lastName,
        string? nationalId = null,
        string? email = null,
        string? phone = null,
        string? occupation = null,
        string? addressLine = null,
        Guid? createdBy = null)
    {
        return new Guardian
        {
            CorporationId = corporationId,
            FirstName     = firstName,
            LastName      = lastName,
            NationalId    = nationalId,
            Email         = email,
            Phone         = phone,
            Occupation    = occupation,
            AddressLine   = addressLine,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow,
            CreatedBy     = createdBy
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void UpdateContactInfo(
        string firstName,
        string lastName,
        string? nationalId,
        string? email,
        string? phone,
        string? occupation,
        string? addressLine,
        Guid? updatedBy = null)
    {
        FirstName   = firstName;
        LastName    = lastName;
        NationalId  = nationalId;
        Email       = email;
        Phone       = phone;
        Occupation  = occupation;
        AddressLine = addressLine;
        UpdatedAt   = DateTimeOffset.UtcNow;
        UpdatedBy   = updatedBy;
    }

    /// <summary>
    /// Links an IAM user account, enabling parent portal login for this guardian.
    /// The portal visibility switches per student are configured separately in GuardianPortalAccess.
    /// </summary>
    public void LinkPortalAccount(Guid userId, Guid? updatedBy = null)
    {
        UserId    = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Removes the portal account link. Portal access switches are revoked separately.</summary>
    public void UnlinkPortalAccount(Guid? updatedBy = null)
    {
        UserId    = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
