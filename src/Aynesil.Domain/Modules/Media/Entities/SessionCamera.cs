namespace Aynesil.Domain.Modules.Media.Entities;

/// <summary>
/// Maps to media.session_camera.
/// Junction entity assigning a camera to a session (temporary, per-session assignment).
/// DB unique constraint: (session_id, camera_id).
///
/// DDL notes: no audit columns — only id, corporation_id, session_id, camera_id.
/// Extends BaseEntity only; all AuditableEntity / SoftDeleteEntity fields ignored in EF config.
/// </summary>
public class SessionCamera : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid CameraId { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SessionCamera Assign(Guid corporationId, Guid sessionId, Guid cameraId)
        => new() { CorporationId = corporationId, SessionId = sessionId, CameraId = cameraId };
}
