using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Developmental profile snapshot for a student by area (cognitive, motor, social, etc.).
/// Maps to students.developmental_profile.
///
/// No soft delete in DB schema — profiles are versioned by inserting a new record per assessment.
/// DeletedAt is NOT present in the DB — ignored in EF configuration.
///
/// Audit: created_at, created_by, updated_at, updated_by, row_version (full — DB column updated_by exists).
/// </summary>
public class DevelopmentalProfile : TenantEntity
{
    public Guid StudentId { get; set; }

    /// <summary>FK to ref.ref_value (ref_type: development_area). Configurable. e.g. Cognitive, Motor, Social.</summary>
    public Guid? DevelopmentAreaId { get; set; }

    public string? Summary { get; set; }
    public string? Strengths { get; set; }
    public string? Needs { get; set; }

    /// <summary>Date the developmental assessment was conducted.</summary>
    public DateOnly? AssessedOn { get; set; }
}
