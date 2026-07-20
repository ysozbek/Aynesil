namespace Aynesil.Domain.Modules.Media.Entities;

/// <summary>
/// Maps to media.viewing_authorization.
/// Time-boxed authorization granting a guardian access to view a student's camera feed.
/// Backed by a legal.student_consent (KVKK camera_viewing) record.
///
/// Authorization lifecycle: granted → (optionally) revoked.
/// Access is time-limited: valid_from / valid_to define the window; IsCurrentlyValid() evaluates it.
///
/// DDL notes:
///   - No updated_at, updated_by, row_version, deleted_at — all ignored in EF config.
///   - access_type_id added via V20 migration.
///   - Immutable once granted; Revoke() is the only state mutation.
/// </summary>
public class ViewingAuthorization : TenantEntity
{
    public Guid GuardianId { get; private set; }
    public Guid StudentId { get; private set; }

    /// <summary>Specific session this authorization covers. NULL = standing authorization for all sessions.</summary>
    public Guid? SessionId { get; private set; }

    /// <summary>FK to legal.student_consent (camera_viewing consent backing this grant).</summary>
    public Guid? ConsentId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'access_type'). Configurable.</summary>
    public Guid? AccessTypeId { get; private set; }

    public DateTimeOffset ValidFrom { get; private set; }

    /// <summary>Must be set — time-limited access is mandatory per authorization policy.</summary>
    public DateTimeOffset ValidTo { get; private set; }

    public Guid? GrantedBy { get; private set; }
    public bool IsRevoked { get; private set; }

    // ── Computed ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when this authorization is currently valid:
    /// not revoked, valid_from has passed, and valid_to has not yet been reached.
    /// </summary>
    public bool IsCurrentlyValid(DateTimeOffset now)
        => !IsRevoked && ValidFrom <= now && now < ValidTo;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ViewingAuthorization Grant(
        Guid corporationId,
        Guid guardianId,
        Guid studentId,
        DateTimeOffset validFrom,
        DateTimeOffset validTo,
        Guid? sessionId = null,
        Guid? consentId = null,
        Guid? accessTypeId = null,
        Guid? grantedBy = null)
    {
        if (validTo <= validFrom)
            throw new ArgumentException("ValidTo must be after ValidFrom.");

        return new ViewingAuthorization
        {
            CorporationId = corporationId,
            GuardianId    = guardianId,
            StudentId     = studentId,
            SessionId     = sessionId,
            ConsentId     = consentId,
            AccessTypeId  = accessTypeId,
            ValidFrom     = validFrom,
            ValidTo       = validTo,
            GrantedBy     = grantedBy,
            IsRevoked     = false
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Revoke()
    {
        if (IsRevoked)
            throw new InvalidOperationException("Viewing authorization is already revoked.");

        IsRevoked = true;
    }
}
