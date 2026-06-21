namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// A schedulable space: physical therapy room, classroom, or virtual/online room.
/// room_type_id references ref.ref_value (ref_type 'room_type') — configurable.
/// Unique per corporation + campus + code.
///
/// Maps to scheduling.room.
/// Audit: created_at, updated_at, deleted_at, row_version (no created_by / updated_by in DDL).
/// </summary>
public class Room : TenantEntity
{
    public Guid? CampusId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'room_type'). Configurable.</summary>
    public Guid? RoomTypeId { get; private set; }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    /// <summary>Maximum number of participants. 0 = unlimited (e.g. online rooms).</summary>
    public int Capacity { get; private set; } = 1;

    /// <summary>True for virtual/online rooms; null campus_id is allowed for these.</summary>
    public bool IsVirtual { get; private set; }

    /// <summary>Video conference join URL for online rooms.</summary>
    public string? MeetingUrl { get; private set; }

    public bool IsActive { get; private set; } = true;

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<Session> Sessions { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Room Create(
        Guid corporationId,
        string code,
        string name,
        int capacity,
        bool isVirtual,
        Guid? campusId = null,
        Guid? roomTypeId = null,
        string? meetingUrl = null)
    {
        if (capacity < 0)
            throw new ArgumentException("Room capacity cannot be negative.");

        if (isVirtual && campusId is not null)
            throw new ArgumentException("Virtual rooms must not be assigned to a campus.");

        return new Room
        {
            CorporationId = corporationId,
            CampusId      = campusId,
            RoomTypeId    = roomTypeId,
            Code          = code,
            Name          = name,
            Capacity      = capacity,
            IsVirtual     = isVirtual,
            MeetingUrl    = meetingUrl,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void UpdateDetails(
        string code,
        string name,
        int capacity,
        Guid? roomTypeId,
        string? meetingUrl)
    {
        if (capacity < 0)
            throw new ArgumentException("Room capacity cannot be negative.");

        Code       = code;
        Name       = name;
        Capacity   = capacity;
        RoomTypeId = roomTypeId;
        MeetingUrl = meetingUrl;
        UpdatedAt  = DateTimeOffset.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Activate()   { IsActive = true;  UpdatedAt = DateTimeOffset.UtcNow; }
}
