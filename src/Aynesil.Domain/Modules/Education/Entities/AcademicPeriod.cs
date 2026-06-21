namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// An academic/academic period (term) used to scope education plans.
/// Only one period per corporation should be marked as current (is_current = true).
/// Term type (academic_term) is configurable reference data.
/// Maps to education.academic_period.
///
/// Audit: created_at, updated_at, row_version.
/// Absent from DDL (ignored in config): created_by, updated_by, deleted_at.
/// </summary>
public class AcademicPeriod : TenantEntity
{
    public string Name { get; private set; } = string.Empty;

    /// <summary>FK to ref.ref_value (ref_type 'academic_term'). Configurable.</summary>
    public Guid? TermId { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    /// <summary>True for the period currently active. Only one per corporation at a time.</summary>
    public bool IsCurrent { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<EducationPlan> EducationPlans { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AcademicPeriod Create(
        Guid corporationId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        Guid? termId = null,
        bool isCurrent = false)
    {
        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.");

        return new AcademicPeriod
        {
            CorporationId = corporationId,
            Name          = name,
            TermId        = termId,
            StartDate     = startDate,
            EndDate       = endDate,
            IsCurrent     = isCurrent,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Update(string name, DateOnly startDate, DateOnly endDate, Guid? termId)
    {
        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.");

        Name      = name;
        TermId    = termId;
        StartDate = startDate;
        EndDate   = endDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsCurrent()
    {
        IsCurrent = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UnmarkAsCurrent()
    {
        IsCurrent = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
