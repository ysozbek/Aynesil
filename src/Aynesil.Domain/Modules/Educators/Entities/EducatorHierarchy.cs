using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Educators.Entities;

/// <summary>
/// Directed edge in the educator supervisory graph.
/// Models any flexible hierarchy (educator → consultant → coordinator) without
/// hardcoding hierarchy levels. Depth is unbounded — callers must guard against cycles.
///
/// relationship_id carries the edge type (ref_type 'educator_relationship'):
/// e.g. 'supervises', 'consults_for', 'coordinates'.
/// campus_id scopes the relationship to a specific campus when needed.
///
/// Maps to educators.educator_hierarchy.
/// Unique (NULLS NOT DISTINCT): (educator_id, supervisor_id, relationship_id, campus_id).
/// Check: educator_id != supervisor_id.
/// No audit columns — inherits only BaseEntity (Id).
/// </summary>
public class EducatorHierarchy : BaseEntity
{
    public Guid CorporationId { get; set; }

    /// <summary>The supervised/subordinate educator.</summary>
    public Guid EducatorId { get; set; }

    /// <summary>The supervisor educator.</summary>
    public Guid SupervisorId { get; set; }

    /// <summary>FK to ref.ref_value (ref_type 'educator_relationship'). Configurable edge type.</summary>
    public Guid? RelationshipId { get; set; }

    /// <summary>When set, the relationship is scoped to a specific campus.</summary>
    public Guid? CampusId { get; set; }

    public DateOnly ActiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>NULL = active indefinitely. Set to terminate the supervisory relationship.</summary>
    public DateOnly? ActiveTo { get; set; }

    /// <summary>Computed helper — not stored in DB.</summary>
    public bool IsActive => ActiveTo == null || ActiveTo >= DateOnly.FromDateTime(DateTime.UtcNow);
}
