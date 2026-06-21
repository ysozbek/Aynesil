using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Educators.Entities;

/// <summary>
/// Many-to-many link between an educator and a specialty.
/// Specialties are configurable reference data (ref_type 'specialty') — business users
/// define new areas (e.g. "Autism Spectrum", "DIR Floortime", "AAC") without code changes.
/// Maps to educators.educator_specialty.
/// Unique per (educator_id, specialty_id).
/// No audit columns — inherits only BaseEntity (Id).
/// </summary>
public class EducatorSpecialty : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid EducatorId { get; set; }

    /// <summary>FK to ref.ref_value (ref_type 'specialty'). Fully configurable per tenant.</summary>
    public Guid SpecialtyId { get; set; }
}
