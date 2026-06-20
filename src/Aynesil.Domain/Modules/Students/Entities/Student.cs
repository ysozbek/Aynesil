using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Students.Events;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Student master record. Lifecycle status is driven by configurable ref_value (student_status),
/// not an enum, so any business user can define new statuses without a code change.
/// Maps to students.student.
///
/// Multi-campus: a student can receive services at multiple campuses via StudentCampus join table.
/// Lead origin: set once on conversion from a CRM lead via SetLeadOrigin().
/// Status: all transitions append a StudentStatusHistory record; the domain event
/// notifies other modules (notifications, audit, etc.).
///
/// Audit: full (created_at, created_by, updated_at, updated_by).
/// Soft delete: deleted_at.
/// </summary>
public class Student : TenantEntity
{
    /// <summary>Human-friendly per-tenant code, e.g. "S-2024-001". Unique per corporation.</summary>
    public string? StudentNo { get; private set; }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    /// <summary>KVKK Art.6 special-category data. Consider encryption at rest.</summary>
    public string? NationalId { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    /// <summary>Free text (e.g. "E", "K", or localised values stored in UI). Not an enum.</summary>
    public string? Gender { get; private set; }

    /// <summary>FK to core.campus — primary service location.</summary>
    public Guid? PrimaryCampusId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type: student_status). Configurable lifecycle status.</summary>
    public Guid? StatusId { get; private set; }

    /// <summary>FK to crm.lead — origin lead. Set once during lead→student conversion, never overwritten.</summary>
    public Guid? LeadId { get; private set; }

    /// <summary>FK to core.file_object — student profile photo.</summary>
    public Guid? PhotoFileId { get; private set; }

    public string? Notes { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<StudentStatusHistory> StatusHistory { get; private set; } = [];
    public ICollection<StudentCampus> Campuses { get; private set; } = [];
    public ICollection<StudentGuardian> Guardians { get; private set; } = [];
    public ICollection<EmergencyContact> EmergencyContacts { get; private set; } = [];
    public ICollection<DevelopmentalProfile> DevelopmentalProfiles { get; private set; } = [];
    public ICollection<Diagnosis> Diagnoses { get; private set; } = [];
    public ICollection<MedicalReport> MedicalReports { get; private set; } = [];
    public ICollection<DevelopmentReport> DevelopmentReports { get; private set; } = [];
    public ICollection<ExternalInstitutionReport> ExternalInstitutionReports { get; private set; } = [];
    public ICollection<CaseNote> CaseNotes { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Student Create(
        Guid corporationId,
        string firstName,
        string lastName,
        string? studentNo = null,
        string? nationalId = null,
        DateOnly? birthDate = null,
        string? gender = null,
        Guid? primaryCampusId = null,
        Guid? statusId = null,
        Guid? leadId = null,
        string? notes = null,
        Guid? createdBy = null)
    {
        var student = new Student
        {
            CorporationId   = corporationId,
            FirstName       = firstName,
            LastName        = lastName,
            StudentNo       = studentNo,
            NationalId      = nationalId,
            BirthDate       = birthDate,
            Gender          = gender,
            PrimaryCampusId = primaryCampusId,
            StatusId        = statusId,
            LeadId          = leadId,
            Notes           = notes,
            CreatedAt       = DateTimeOffset.UtcNow,
            UpdatedAt       = DateTimeOffset.UtcNow,
            CreatedBy       = createdBy
        };

        student.AddDomainEvent(new StudentCreatedEvent(
            student.Id, corporationId, firstName, lastName, leadId, createdBy));

        return student;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void UpdateProfile(
        string firstName,
        string lastName,
        string? studentNo,
        string? nationalId,
        DateOnly? birthDate,
        string? gender,
        Guid? primaryCampusId,
        string? notes,
        Guid? updatedBy = null)
    {
        FirstName       = firstName;
        LastName        = lastName;
        StudentNo       = studentNo;
        NationalId      = nationalId;
        BirthDate       = birthDate;
        Gender          = gender;
        PrimaryCampusId = primaryCampusId;
        Notes           = notes;
        UpdatedAt       = DateTimeOffset.UtcNow;
        UpdatedBy       = updatedBy;
    }

    /// <summary>
    /// Changes the student's lifecycle status and returns a new immutable history record.
    /// Caller must persist the returned history record.
    /// The caller is responsible for validating that newStatusId is a valid student_status ref_value.
    /// </summary>
    public StudentStatusHistory ChangeStatus(
        Guid newStatusId,
        string? reason = null,
        Guid? changedBy = null)
    {
        var previous = StatusId;
        StatusId  = newStatusId;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = changedBy;

        var history = new StudentStatusHistory
        {
            CorporationId = CorporationId,
            StudentId     = Id,
            StatusId      = newStatusId,
            Reason        = reason,
            ChangedAt     = DateTimeOffset.UtcNow,
            ChangedBy     = changedBy
        };

        AddDomainEvent(new StudentStatusChangedEvent(Id, CorporationId, previous, newStatusId, changedBy));

        return history;
    }

    public void SetPhoto(Guid? photoFileId, Guid? updatedBy = null)
    {
        PhotoFileId = photoFileId;
        UpdatedAt   = DateTimeOffset.UtcNow;
        UpdatedBy   = updatedBy;
    }

    /// <summary>
    /// Records the CRM lead from which this student was converted.
    /// Can only be set once. The CRM module calls this after creating the student.
    /// </summary>
    public void SetLeadOrigin(Guid leadId)
    {
        if (LeadId.HasValue)
            throw new InvalidOperationException(
                $"Student {Id} already has an origin lead (LeadId={LeadId}). Cannot re-link.");

        LeadId    = leadId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
