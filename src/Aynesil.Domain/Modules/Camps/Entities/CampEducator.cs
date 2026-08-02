using Aynesil.Domain.Modules.Camps.Events;

namespace Aynesil.Domain.Modules.Camps.Entities;

/// <summary>
/// Maps to camps.camp_educator.
/// Educator assignment scoped to a camp, optionally narrowed to a period and/or activity.
/// Role: 'lead' | 'assistant' | 'observer' | 'supervisor' (mirrors scheduling.session_educator).
/// Extends BaseEntity — no soft-delete / audit columns in DDL.
/// </summary>
public class CampEducator : BaseEntity
{
    private static readonly string[] ValidRoles = ["lead", "assistant", "observer", "supervisor"];

    public Guid CorporationId { get; private set; }
    public Guid CampId { get; private set; }
    public Guid? CampPeriodId { get; private set; }
    public Guid? CampActivityId { get; private set; }
    public Guid EducatorId { get; private set; }

    /// <summary>'lead' | 'assistant' | 'observer' | 'supervisor'</summary>
    public string Role { get; private set; } = "lead";

    public DateTimeOffset AssignedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? AssignedBy { get; private set; }

    public Camp Camp { get; private set; } = null!;
    public CampPeriod? Period { get; private set; }
    public CampActivity? Activity { get; private set; }

    public static CampEducator Assign(
        Guid corporationId,
        Guid campId,
        Guid educatorId,
        string role = "lead",
        Guid? campPeriodId = null,
        Guid? campActivityId = null,
        Guid? assignedBy = null)
    {
        if (!ValidRoles.Contains(role))
            throw new ArgumentException(
                $"Invalid role '{role}'. Valid: {string.Join(", ", ValidRoles)}");

        var assignment = new CampEducator
        {
            CorporationId  = corporationId,
            CampId         = campId,
            CampPeriodId   = campPeriodId,
            CampActivityId = campActivityId,
            EducatorId     = educatorId,
            Role           = role,
            AssignedAt     = DateTimeOffset.UtcNow,
            AssignedBy     = assignedBy
        };

        assignment.AddDomainEvent(new CampEducatorAssignedEvent(
            assignment.Id, corporationId, campId, educatorId, role));

        return assignment;
    }

    public void UpdateRole(string role)
    {
        if (!ValidRoles.Contains(role))
            throw new ArgumentException(
                $"Invalid role '{role}'. Valid: {string.Join(", ", ValidRoles)}");
        Role = role;
    }
}
