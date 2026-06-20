using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Emergency contact for a student. Not a portal user; for emergency notification only.
/// Maps to students.emergency_contact.
/// No audit, no soft delete — replaced as a set when contacts are updated.
/// </summary>
public class EmergencyContact : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid StudentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>Free-text relationship description (e.g. "Aunt", "Neighbour").</summary>
    public string? Relationship { get; set; }

    public string Phone { get; set; } = string.Empty;

    /// <summary>Call priority: 1 = first contact, 2 = second, etc.</summary>
    public int Priority { get; set; } = 1;
}
