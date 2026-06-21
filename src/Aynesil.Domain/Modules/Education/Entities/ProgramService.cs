namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// A service line within a program (e.g. "ABA Therapy", "Speech-Language Therapy").
/// Service type (service_type) is configurable reference data:
/// therapy, education, consultation, camp, online.
/// Maps to education.program_service.
/// No audit columns — inherits only BaseEntity (Id).
/// </summary>
public class ProgramService : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid ProgramId { get; set; }

    /// <summary>FK to ref.ref_value (ref_type 'service_type'). Configurable.</summary>
    public Guid? ServiceTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Default session length in minutes used when scheduling a session for this service.</summary>
    public int? DefaultDurationMinutes { get; set; }

    /// <summary>Target cadence for scheduling (e.g. 2.5 = 2.5 sessions per week).</summary>
    public decimal? DefaultSessionsPerWeek { get; set; }

    public int SortOrder { get; set; }

    // Navigation
    public EducationProgram Program { get; set; } = null!;
}
