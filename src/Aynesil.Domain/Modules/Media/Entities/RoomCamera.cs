namespace Aynesil.Domain.Modules.Media.Entities;

/// <summary>
/// Maps to media.room_camera.
/// Junction entity assigning a camera to a room (physical location assignment).
/// DB unique constraint: (room_id, camera_id).
///
/// DDL notes: no audit columns — only id, corporation_id, room_id, camera_id.
/// Extends BaseEntity only; all AuditableEntity / SoftDeleteEntity fields ignored in EF config.
/// </summary>
public class RoomCamera : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid CameraId { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static RoomCamera Assign(Guid corporationId, Guid roomId, Guid cameraId)
        => new() { CorporationId = corporationId, RoomId = roomId, CameraId = cameraId };
}
