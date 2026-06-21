using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Programs.Dtos;

// ── Program DTOs ──────────────────────────────────────────────────────────────

public record ProgramDto(
    Guid Id,
    Guid CorporationId,
    string Code,
    string Name,
    Guid? ProgramTypeId,
    string? ProgramTypeLabel,
    string? Description,
    bool IsActive,
    int RowVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ProgramServiceDto> Services,
    IReadOnlyList<ProgramTranslationDto> Translations);

public record ProgramListItemDto(
    Guid Id,
    Guid CorporationId,
    string Code,
    string Name,
    Guid? ProgramTypeId,
    string? ProgramTypeLabel,
    string? Description,
    bool IsActive,
    int ServiceCount,
    DateTimeOffset CreatedAt);

// ── Program Service DTOs ──────────────────────────────────────────────────────

public record ProgramServiceDto(
    Guid Id,
    Guid? ServiceTypeId,
    string? ServiceTypeLabel,
    string Name,
    int? DefaultDurationMinutes,
    decimal? DefaultSessionsPerWeek,
    int SortOrder);

// ── Program Translation DTO ───────────────────────────────────────────────────

public record ProgramTranslationDto(
    string Locale,
    string Name,
    string? Description);

// ── Enrollment DTOs ───────────────────────────────────────────────────────────

public record EnrollmentDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    string? StudentFullName,
    Guid? CampusId,
    string? CampusName,
    Guid? StatusId,
    string? StatusLabel,
    DateOnly EnrolledOn,
    DateOnly? EndedOn,
    string? TerminationReason,
    int RowVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<StudentProgramDto> StudentPrograms);

public record EnrollmentListItemDto(
    Guid Id,
    Guid StudentId,
    string? StudentFullName,
    Guid? CampusId,
    string? CampusName,
    Guid? StatusId,
    string? StatusLabel,
    DateOnly EnrolledOn,
    DateOnly? EndedOn,
    int ProgramCount);

// ── StudentProgram DTOs ───────────────────────────────────────────────────────

public record StudentProgramDto(
    Guid Id,
    Guid StudentId,
    Guid ProgramId,
    string? ProgramName,
    string? ProgramCode,
    Guid? EnrollmentId,
    Guid? CampusId,
    string? CampusName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Status,
    int RowVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record StudentProgramListItemDto(
    Guid Id,
    Guid StudentId,
    Guid ProgramId,
    string ProgramName,
    string ProgramCode,
    string? ProgramTypeLabel,
    Guid? CampusId,
    string? CampusName,
    DateOnly? StartDate,
    string Status,
    DateTimeOffset CreatedAt);

// ── Projection Helper ─────────────────────────────────────────────────────────

internal static class ProgramProjection
{
    public static async Task<ProgramDto?> LoadAsync(
        IAppDbContext db, Guid programId, CancellationToken ct)
    {
        var program = await db.EducationPrograms
            .AsNoTracking()
            .Include(p => p.Services)
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == programId, ct);

        if (program is null) return null;

        var typeLabel = program.ProgramTypeId.HasValue
            ? await db.RefValues.AsNoTracking()
                .Where(r => r.Id == program.ProgramTypeId.Value)
                .Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var serviceDtos = new List<ProgramServiceDto>();
        foreach (var s in program.Services)
        {
            var stLabel = s.ServiceTypeId.HasValue
                ? await db.RefValues.AsNoTracking()
                    .Where(r => r.Id == s.ServiceTypeId.Value)
                    .Select(r => r.Code).FirstOrDefaultAsync(ct)
                : null;
            serviceDtos.Add(new ProgramServiceDto(
                s.Id, s.ServiceTypeId, stLabel, s.Name,
                s.DefaultDurationMinutes, s.DefaultSessionsPerWeek, s.SortOrder));
        }

        var translationDtos = program.Translations
            .Select(t => new ProgramTranslationDto(t.Locale, t.Name, t.Description))
            .ToList();

        return new ProgramDto(
            program.Id, program.CorporationId, program.Code, program.Name,
            program.ProgramTypeId, typeLabel, program.Description, program.IsActive,
            program.RowVersion, program.CreatedAt, program.UpdatedAt,
            serviceDtos, translationDtos);
    }

    public static async Task<EnrollmentDto?> LoadEnrollmentAsync(
        IAppDbContext db, Guid enrollmentId, CancellationToken ct)
    {
        var enrollment = await db.Enrollments
            .AsNoTracking()
            .Include(e => e.StudentPrograms)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

        if (enrollment is null) return null;

        var studentName = await db.Students.AsNoTracking()
            .Where(s => s.Id == enrollment.StudentId)
            .Select(s => s.FirstName + " " + s.LastName).FirstOrDefaultAsync(ct);

        var campusName = enrollment.CampusId.HasValue
            ? await db.Campuses.AsNoTracking()
                .Where(c => c.Id == enrollment.CampusId.Value)
                .Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;

        var statusLabel = enrollment.StatusId.HasValue
            ? await db.RefValues.AsNoTracking()
                .Where(r => r.Id == enrollment.StatusId.Value)
                .Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var spDtos = new List<StudentProgramDto>();
        foreach (var sp in enrollment.StudentPrograms.Where(s => s.DeletedAt == null))
        {
            var prog = await db.EducationPrograms.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == sp.ProgramId, ct);
            var spCampusName = sp.CampusId.HasValue
                ? await db.Campuses.AsNoTracking()
                    .Where(c => c.Id == sp.CampusId.Value)
                    .Select(c => c.Name).FirstOrDefaultAsync(ct)
                : null;
            spDtos.Add(new StudentProgramDto(
                sp.Id, sp.StudentId, sp.ProgramId,
                prog?.Name, prog?.Code,
                sp.EnrollmentId, sp.CampusId, spCampusName,
                sp.StartDate, sp.EndDate, sp.Status,
                sp.RowVersion, sp.CreatedAt, sp.UpdatedAt));
        }

        return new EnrollmentDto(
            enrollment.Id, enrollment.CorporationId, enrollment.StudentId, studentName,
            enrollment.CampusId, campusName,
            enrollment.StatusId, statusLabel,
            enrollment.EnrolledOn, enrollment.EndedOn, enrollment.TerminationReason,
            enrollment.RowVersion, enrollment.CreatedAt, enrollment.UpdatedAt,
            spDtos);
    }

    public static StudentProgramDto ToStudentProgramDto(StudentProgram sp, string? programName, string? programCode, string? campusName)
        => new(sp.Id, sp.StudentId, sp.ProgramId, programName, programCode,
            sp.EnrollmentId, sp.CampusId, campusName,
            sp.StartDate, sp.EndDate, sp.Status,
            sp.RowVersion, sp.CreatedAt, sp.UpdatedAt);
}
