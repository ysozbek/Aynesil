using System.Net;

namespace Aynesil.Domain.Modules.Media.Entities;

/// <summary>
/// Maps to media.viewing_log.
/// Immutable, append-only privacy and audit log of who watched what camera feed, when.
/// PARTITIONED by range (started_at). Default partition: viewing_log_default.
///
/// DDL notes:
///   - PK is composite: (id bigint GENERATED ALWAYS AS IDENTITY, started_at).
///   - Does NOT extend BaseEntity (UUID PK assumed there); standalone entity.
///   - ip_address maps to PostgreSQL inet via Npgsql (System.Net.IPAddress).
///   - No update operations — immutable after creation; EndAsync() issues a direct UPDATE.
/// </summary>
public class ViewingLog
{
    /// <summary>Auto-generated bigint identity. Assigned by the database on insert.</summary>
    public long Id { get; private set; }

    public Guid CorporationId { get; private set; }
    public Guid? GuardianId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? SessionId { get; private set; }
    public Guid? CameraId { get; private set; }
    public Guid? AuthorizationId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public IPAddress? IpAddress { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ViewingLog Start(
        Guid corporationId,
        Guid? guardianId,
        Guid? userId,
        Guid? sessionId,
        Guid? cameraId,
        Guid? authorizationId,
        IPAddress? ipAddress = null)
        => new()
        {
            CorporationId    = corporationId,
            GuardianId       = guardianId,
            UserId           = userId,
            SessionId        = sessionId,
            CameraId         = cameraId,
            AuthorizationId  = authorizationId,
            StartedAt        = DateTimeOffset.UtcNow,
            IpAddress        = ipAddress
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void End(DateTimeOffset endedAt)
    {
        if (EndedAt.HasValue)
            throw new InvalidOperationException("Viewing session has already been ended.");

        if (endedAt <= StartedAt)
            throw new ArgumentException("EndedAt must be after StartedAt.");

        EndedAt = endedAt;
    }
}
