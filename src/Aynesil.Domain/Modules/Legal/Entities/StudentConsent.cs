namespace Aynesil.Domain.Modules.Legal.Entities;

/// <summary>
/// Maps to legal.student_consent.
/// KVKK consent ledger: each row represents a consent grant or withdrawal for a student/guardian.
/// Scope examples: data_processing, camera_viewing, media.
///
/// This is a minimal read model introduced to support consent validation in other modules
/// (e.g., media.viewing_authorization). Full Legal module implementation is deferred.
///
/// DDL notes:
///   - No deleted_at — physical delete is forbidden; state transitions are the lifecycle.
///   - state: 'granted' | 'withdrawn' | 'expired'.
///   - valid_until is a DATE (DateOnly).
/// </summary>
public class StudentConsent : TenantEntity
{
    public Guid StudentId { get; private set; }
    public Guid? GuardianId { get; private set; }
    public Guid? ConsentTypeId { get; private set; }

    /// <summary>'granted' | 'withdrawn' | 'expired'</summary>
    public string State { get; private set; } = "granted";

    public DateTimeOffset? GrantedAt { get; private set; }
    public DateTimeOffset? WithdrawnAt { get; private set; }

    /// <summary>Optional expiry date. NULL = never expires.</summary>
    public DateOnly? ValidUntil { get; private set; }
}
