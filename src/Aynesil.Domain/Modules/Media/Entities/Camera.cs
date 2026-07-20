namespace Aynesil.Domain.Modules.Media.Entities;

/// <summary>
/// Maps to media.camera.
/// Represents a physical or virtual camera registered within a corporation/campus.
/// Camera type is configurable via ref_value (ref_type 'camera_type').
/// stream_provider_id references core.integration_connection (vendor-agnostic streaming provider).
///
/// DDL notes:
///   - created_by / updated_by columns are NOT present — EF configuration ignores them.
///   - deleted_at IS present — soft-delete supported via SoftDelete().
///   - row_version IS present — concurrency token.
///   - camera_type_id added via V20 migration.
/// </summary>
public class Camera : TenantEntity
{
    public Guid? CampusId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'camera_type'). Configurable.</summary>
    public Guid? CameraTypeId { get; private set; }

    /// <summary>Unique code within the corporation (DB unique constraint).</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>FK to core.integration_connection (streaming provider).</summary>
    public Guid? StreamProviderId { get; private set; }

    /// <summary>Provider-specific stream identifier. Does not contain raw secrets.</summary>
    public string? StreamRef { get; private set; }

    public bool IsActive { get; private set; } = true;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Camera Register(
        Guid corporationId,
        string code,
        string name,
        Guid? campusId = null,
        Guid? cameraTypeId = null,
        Guid? streamProviderId = null,
        string? streamRef = null,
        Guid? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Camera
        {
            CorporationId    = corporationId,
            Code             = code.Trim(),
            Name             = name.Trim(),
            CampusId         = campusId,
            CameraTypeId     = cameraTypeId,
            StreamProviderId = streamProviderId,
            StreamRef        = streamRef,
            IsActive         = true,
            CreatedBy        = createdBy
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Update(
        string name,
        Guid? campusId,
        Guid? cameraTypeId,
        Guid? streamProviderId,
        string? streamRef,
        Guid? updatedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name             = name.Trim();
        CampusId         = campusId;
        CameraTypeId     = cameraTypeId;
        StreamProviderId = streamProviderId;
        StreamRef        = streamRef;
        UpdatedAt        = DateTimeOffset.UtcNow;
        UpdatedBy        = updatedBy;
    }

    public void Activate(Guid? updatedBy = null)
    {
        IsActive  = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(Guid? updatedBy = null)
    {
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
