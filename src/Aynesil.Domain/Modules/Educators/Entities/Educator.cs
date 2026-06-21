using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Educators.Events;

namespace Aynesil.Domain.Modules.Educators.Entities;

/// <summary>
/// Educator master record. Title (educator_title) is configurable reference data —
/// therapist, psychologist, consultant, coordinator, or any type a business user adds.
/// Employment type is a free text constraint: full_time, part_time, or contractor.
/// Maps to educators.educator.
///
/// Multi-campus: an educator can work across campuses via EducatorCampus join table.
/// Hierarchy: supervisory relationships are stored in EducatorHierarchy (directed graph).
/// Portal login: optional user_id links to an iam.user_account for future educator portal.
///
/// Audit: full (created_at, created_by, updated_at, updated_by).
/// Soft delete: deleted_at.
/// Concurrency: row_version.
/// </summary>
public class Educator : TenantEntity
{
    /// <summary>FK to iam.user_account.id — set when educator has a portal/system login.</summary>
    public Guid? UserId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    /// <summary>FK to ref.ref_value (ref_type 'educator_title'). Fully configurable.</summary>
    public Guid? TitleId { get; private set; }

    /// <summary>citext in DB — case-insensitive uniqueness is enforced at DB level.</summary>
    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    /// <summary>full_time | part_time | contractor. Validated in commands.</summary>
    public string? EmploymentType { get; private set; }

    public DateOnly? HireDate { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>FK to core.campus — primary working location.</summary>
    public Guid? PrimaryCampusId { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<EducatorCampus> Campuses { get; private set; } = [];
    public ICollection<EducatorSpecialty> Specialties { get; private set; } = [];
    public ICollection<EducatorCertification> Certifications { get; private set; } = [];

    /// <summary>Edges where this educator is the subordinate.</summary>
    public ICollection<EducatorHierarchy> Supervisors { get; private set; } = [];

    /// <summary>Edges where this educator is the supervisor.</summary>
    public ICollection<EducatorHierarchy> Subordinates { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Educator Create(
        Guid corporationId,
        string firstName,
        string lastName,
        Guid? titleId = null,
        string? email = null,
        string? phone = null,
        string? employmentType = null,
        DateOnly? hireDate = null,
        Guid? primaryCampusId = null,
        Guid? createdBy = null)
    {
        var educator = new Educator
        {
            CorporationId   = corporationId,
            FirstName       = firstName,
            LastName        = lastName,
            TitleId         = titleId,
            Email           = email,
            Phone           = phone,
            EmploymentType  = employmentType,
            HireDate        = hireDate,
            PrimaryCampusId = primaryCampusId,
            IsActive        = true,
            CreatedAt       = DateTimeOffset.UtcNow,
            UpdatedAt       = DateTimeOffset.UtcNow,
            CreatedBy       = createdBy
        };

        educator.AddDomainEvent(new EducatorCreatedEvent(
            educator.Id, corporationId, firstName, lastName, titleId, createdBy));

        return educator;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void UpdateProfile(
        string firstName,
        string lastName,
        Guid? titleId,
        string? email,
        string? phone,
        string? employmentType,
        DateOnly? hireDate,
        Guid? primaryCampusId,
        Guid? updatedBy = null)
    {
        FirstName       = firstName;
        LastName        = lastName;
        TitleId         = titleId;
        Email           = email;
        Phone           = phone;
        EmploymentType  = employmentType;
        HireDate        = hireDate;
        PrimaryCampusId = primaryCampusId;
        UpdatedAt       = DateTimeOffset.UtcNow;
        UpdatedBy       = updatedBy;
    }

    public void Deactivate(Guid? updatedBy = null)
    {
        if (!IsActive) return;
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(new EducatorDeactivatedEvent(Id, CorporationId, updatedBy));
    }

    public void Activate(Guid? updatedBy = null)
    {
        if (IsActive) return;
        IsActive  = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Links an IAM user account enabling system login for this educator.</summary>
    public void LinkUserAccount(Guid userId, Guid? updatedBy = null)
    {
        UserId    = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void UnlinkUserAccount(Guid? updatedBy = null)
    {
        UserId    = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
