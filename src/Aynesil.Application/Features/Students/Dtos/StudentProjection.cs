using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Dtos;

// ── Student DTOs ──────────────────────────────────────────────────────────────

public record StudentDto(
    Guid Id,
    Guid CorporationId,
    string? StudentNo,
    string FirstName,
    string LastName,
    string FullName,
    string? NationalId,
    DateOnly? BirthDate,
    string? Gender,
    Guid? PrimaryCampusId,
    string? PrimaryCampusName,
    Guid? StatusId,
    string? StatusLabel,
    Guid? LeadId,
    Guid? PhotoFileId,
    string? Notes,
    int RowVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<StudentGuardianDto> Guardians,
    IReadOnlyList<EmergencyContactDto> EmergencyContacts,
    IReadOnlyList<StudentCampusDto> Campuses,
    IReadOnlyList<DiagnosisDto> Diagnoses,
    IReadOnlyList<DevelopmentalProfileDto> DevelopmentalProfiles);

public record StudentListItemDto(
    Guid Id,
    string? StudentNo,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly? BirthDate,
    string? Gender,
    Guid? PrimaryCampusId,
    string? PrimaryCampusName,
    Guid? StatusId,
    string? StatusLabel,
    DateTimeOffset CreatedAt);

public record StudentSummaryDto(
    Guid Id,
    string? StudentNo,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly? BirthDate,
    Guid? PhotoFileId,
    Guid? StatusId,
    string? StatusLabel,
    string? PrimaryCampusName);

// ── Guardian DTOs ─────────────────────────────────────────────────────────────

public record GuardianDto(
    Guid Id,
    Guid CorporationId,
    Guid? UserId,
    string FirstName,
    string LastName,
    string FullName,
    string? NationalId,
    string? Email,
    string? Phone,
    string? Occupation,
    string? AddressLine,
    bool HasPortalAccount,
    int RowVersion,
    DateTimeOffset CreatedAt,
    IReadOnlyList<StudentGuardianDto> Students);

public record GuardianListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    bool HasPortalAccount,
    int LinkedStudentCount);

public record StudentGuardianDto(
    Guid LinkId,
    Guid GuardianId,
    string GuardianFullName,
    string? GuardianEmail,
    string? GuardianPhone,
    Guid? RelationshipId,
    string? RelationshipLabel,
    bool IsPrimary,
    bool HasCustody,
    bool PortalAccess,
    bool FinancialResponsible);

// ── Emergency Contact DTOs ────────────────────────────────────────────────────

public record EmergencyContactDto(
    Guid Id,
    string FullName,
    string? Relationship,
    string Phone,
    int Priority);

// ── Campus DTOs ───────────────────────────────────────────────────────────────

public record StudentCampusDto(
    Guid Id,
    Guid CampusId,
    string? CampusName,
    bool IsPrimary,
    DateOnly ActiveFrom,
    DateOnly? ActiveTo,
    bool IsActive);

// ── Developmental Profile DTOs ────────────────────────────────────────────────

public record DevelopmentalProfileDto(
    Guid Id,
    Guid? DevelopmentAreaId,
    string? DevelopmentAreaLabel,
    string? Summary,
    string? Strengths,
    string? Needs,
    DateOnly? AssessedOn,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── Diagnosis DTOs ────────────────────────────────────────────────────────────

public record DiagnosisDto(
    Guid Id,
    Guid StudentId,
    Guid? CategoryId,
    string? CategoryLabel,
    string? IcdCode,
    string? Description,
    DateOnly? DiagnosedOn,
    string? DiagnosedBy,
    Guid? SourceFileId,
    DateTimeOffset CreatedAt,
    int RowVersion);

// ── Report DTOs ───────────────────────────────────────────────────────────────

public record MedicalReportDto(
    Guid Id,
    Guid StudentId,
    string Title,
    DateOnly? ReportDate,
    string? Issuer,
    string? Summary,
    Guid? FileId,
    DateTimeOffset CreatedAt,
    int RowVersion);

public record DevelopmentReportDto(
    Guid Id,
    Guid StudentId,
    string? PeriodLabel,
    DateOnly? ReportDate,
    Guid? AuthoredBy,
    string? Content,
    Guid? FileId,
    DateTimeOffset CreatedAt,
    int RowVersion);

public record ExternalInstitutionReportDto(
    Guid Id,
    Guid StudentId,
    string InstitutionName,
    Guid? InstitutionTypeId,
    string? InstitutionTypeLabel,
    DateOnly? ReportDate,
    string? Summary,
    Guid? FileId,
    DateTimeOffset CreatedAt,
    int RowVersion);

// ── Case Note DTOs ────────────────────────────────────────────────────────────

public record CaseNoteDto(
    Guid Id,
    Guid StudentId,
    string? NoteType,
    string Body,
    bool IsConfidential,
    Guid? AuthoredBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── Portal Access DTOs ────────────────────────────────────────────────────────

public record GuardianPortalAccessDto(
    Guid Id,
    Guid GuardianId,
    Guid StudentId,
    bool CanViewSessions,
    bool CanViewAttendance,
    bool CanViewReports,
    bool CanViewPlan,
    bool CanViewFinance,
    bool CanViewCamera,
    DateTimeOffset GrantedAt,
    DateTimeOffset? RevokedAt,
    bool IsActive);

// ── Status History DTO ────────────────────────────────────────────────────────

public record StudentStatusHistoryDto(
    Guid Id,
    Guid StatusId,
    string? StatusLabel,
    string? Reason,
    DateTimeOffset ChangedAt,
    Guid? ChangedBy);

// ── Portal My Students DTO ────────────────────────────────────────────────────

public record PortalStudentDto(
    Guid StudentId,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly? BirthDate,
    Guid? PhotoFileId,
    string? PrimaryCampusName,
    bool CanViewSessions,
    bool CanViewAttendance,
    bool CanViewReports,
    bool CanViewPlan,
    bool CanViewFinance,
    bool CanViewCamera);

// ── Projection Helper ─────────────────────────────────────────────────────────

internal static class StudentProjection
{
    // ── Student ───────────────────────────────────────────────────────────────

    public static async Task<StudentDto?> LoadStudentAsync(
        IAppDbContext db, Guid studentId, CancellationToken ct)
    {
        var student = await db.Students
            .AsNoTracking()
            .Include(s => s.Guardians)
            .Include(s => s.EmergencyContacts)
            .Include(s => s.Campuses)
            .Include(s => s.Diagnoses)
            .Include(s => s.DevelopmentalProfiles)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student is null) return null;

        var campusName = student.PrimaryCampusId.HasValue
            ? await db.Campuses
                .AsNoTracking()
                .Where(c => c.Id == student.PrimaryCampusId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var statusLabel = student.StatusId.HasValue
            ? await db.RefValues
                .AsNoTracking()
                .Where(r => r.Id == student.StatusId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        var guardianDtos = await BuildGuardianLinkDtosAsync(db, student.Guardians, ct);

        return new StudentDto(
            student.Id, student.CorporationId, student.StudentNo,
            student.FirstName, student.LastName,
            $"{student.FirstName} {student.LastName}",
            student.NationalId, student.BirthDate, student.Gender,
            student.PrimaryCampusId, campusName,
            student.StatusId, statusLabel,
            student.LeadId, student.PhotoFileId, student.Notes,
            student.RowVersion, student.CreatedAt, student.UpdatedAt,
            guardianDtos,
            student.EmergencyContacts.Select(ToEmergencyContactDto).ToList(),
            student.Campuses.Select(ToStudentCampusDto).ToList(),
            student.Diagnoses.Select(ToDiagnosisDto).ToList(),
            student.DevelopmentalProfiles.Select(ToDevelopmentalProfileDto).ToList());
    }

    // ── Guardian ──────────────────────────────────────────────────────────────

    public static async Task<GuardianDto?> LoadGuardianAsync(
        IAppDbContext db, Guid guardianId, CancellationToken ct)
    {
        var guardian = await db.Guardians
            .AsNoTracking()
            .Include(g => g.Students)
            .FirstOrDefaultAsync(g => g.Id == guardianId, ct);

        if (guardian is null) return null;

        var studentLinks = await BuildGuardianStudentLinkDtosAsync(db, guardian.Students, ct);

        return new GuardianDto(
            guardian.Id, guardian.CorporationId, guardian.UserId,
            guardian.FirstName, guardian.LastName,
            $"{guardian.FirstName} {guardian.LastName}",
            guardian.NationalId, guardian.Email, guardian.Phone,
            guardian.Occupation, guardian.AddressLine,
            guardian.UserId.HasValue,
            guardian.RowVersion, guardian.CreatedAt,
            studentLinks);
    }

    // ── Link helpers ──────────────────────────────────────────────────────────

    private static async Task<IReadOnlyList<StudentGuardianDto>> BuildGuardianLinkDtosAsync(
        IAppDbContext db, IEnumerable<StudentGuardian> links, CancellationToken ct)
    {
        var result = new List<StudentGuardianDto>();
        foreach (var link in links)
        {
            var guardian = await db.Guardians.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == link.GuardianId, ct);
            if (guardian is null) continue;

            var relLabel = link.RelationshipId.HasValue
                ? await db.RefValues.AsNoTracking()
                    .Where(r => r.Id == link.RelationshipId.Value)
                    .Select(r => r.Code)
                    .FirstOrDefaultAsync(ct)
                : null;

            result.Add(new StudentGuardianDto(
                link.Id, link.GuardianId,
                $"{guardian.FirstName} {guardian.LastName}",
                guardian.Email, guardian.Phone,
                link.RelationshipId, relLabel,
                link.IsPrimary, link.HasCustody,
                link.PortalAccess, link.FinancialResponsible));
        }
        return result;
    }

    private static async Task<IReadOnlyList<StudentGuardianDto>> BuildGuardianStudentLinkDtosAsync(
        IAppDbContext db, IEnumerable<StudentGuardian> links, CancellationToken ct)
    {
        var result = new List<StudentGuardianDto>();
        foreach (var link in links)
        {
            var guardian = await db.Guardians.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == link.GuardianId, ct);
            if (guardian is null) continue;

            var relLabel = link.RelationshipId.HasValue
                ? await db.RefValues.AsNoTracking()
                    .Where(r => r.Id == link.RelationshipId.Value)
                    .Select(r => r.Code)
                    .FirstOrDefaultAsync(ct)
                : null;

            result.Add(new StudentGuardianDto(
                link.Id, link.GuardianId,
                $"{guardian.FirstName} {guardian.LastName}",
                guardian.Email, guardian.Phone,
                link.RelationshipId, relLabel,
                link.IsPrimary, link.HasCustody,
                link.PortalAccess, link.FinancialResponsible));
        }
        return result;
    }

    // ── Static DTO mappers ────────────────────────────────────────────────────

    public static StudentGuardianDto ToStudentGuardianDto(
        StudentGuardian link, Guardian? guardian)
        => new(link.Id, link.GuardianId,
            guardian is not null ? $"{guardian.FirstName} {guardian.LastName}" : string.Empty,
            guardian?.Email, guardian?.Phone,
            link.RelationshipId, null,
            link.IsPrimary, link.HasCustody, link.PortalAccess, link.FinancialResponsible);

    public static EmergencyContactDto ToEmergencyContactDto(EmergencyContact ec)
        => new(ec.Id, ec.FullName, ec.Relationship, ec.Phone, ec.Priority);

    public static StudentCampusDto ToStudentCampusDto(StudentCampus sc)
        => new(sc.Id, sc.CampusId, null, sc.IsPrimary, sc.ActiveFrom, sc.ActiveTo, sc.IsActive);

    public static DevelopmentalProfileDto ToDevelopmentalProfileDto(DevelopmentalProfile p)
        => new(p.Id, p.DevelopmentAreaId, null, p.Summary, p.Strengths, p.Needs,
            p.AssessedOn, p.CreatedAt, p.UpdatedAt, p.RowVersion);

    public static DiagnosisDto ToDiagnosisDto(Diagnosis d)
        => new(d.Id, d.StudentId, d.CategoryId, null, d.IcdCode, d.Description,
            d.DiagnosedOn, d.DiagnosedBy, d.SourceFileId, d.CreatedAt, d.RowVersion);

    public static MedicalReportDto ToMedicalReportDto(MedicalReport r)
        => new(r.Id, r.StudentId, r.Title, r.ReportDate, r.Issuer, r.Summary,
            r.FileId, r.CreatedAt, r.RowVersion);

    public static DevelopmentReportDto ToDevelopmentReportDto(DevelopmentReport r)
        => new(r.Id, r.StudentId, r.PeriodLabel, r.ReportDate, r.AuthoredBy,
            r.Content, r.FileId, r.CreatedAt, r.RowVersion);

    public static ExternalInstitutionReportDto ToExternalInstitutionReportDto(ExternalInstitutionReport r)
        => new(r.Id, r.StudentId, r.InstitutionName, r.InstitutionTypeId, null,
            r.ReportDate, r.Summary, r.FileId, r.CreatedAt, r.RowVersion);

    public static CaseNoteDto ToCaseNoteDto(CaseNote n)
        => new(n.Id, n.StudentId, n.NoteType, n.Body, n.IsConfidential,
            n.AuthoredBy, n.CreatedAt, n.UpdatedAt, n.RowVersion);

    public static GuardianPortalAccessDto ToPortalAccessDto(GuardianPortalAccess a)
        => new(a.Id, a.GuardianId, a.StudentId,
            a.CanViewSessions, a.CanViewAttendance, a.CanViewReports,
            a.CanViewPlan, a.CanViewFinance, a.CanViewCamera,
            a.GrantedAt, a.RevokedAt, a.IsActive);
}
