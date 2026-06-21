using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Educators.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Educators.Dtos;

// ── Educator DTOs ─────────────────────────────────────────────────────────────

public record EducatorDto(
    Guid Id,
    Guid CorporationId,
    Guid? UserId,
    string FirstName,
    string LastName,
    string FullName,
    Guid? TitleId,
    string? TitleLabel,
    string? Email,
    string? Phone,
    string? EmploymentType,
    DateOnly? HireDate,
    bool IsActive,
    Guid? PrimaryCampusId,
    string? PrimaryCampusName,
    int RowVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<EducatorCampusDto> Campuses,
    IReadOnlyList<EducatorSpecialtyDto> Specialties,
    IReadOnlyList<EducatorCertificationDto> Certifications,
    IReadOnlyList<EducatorHierarchyDto> Supervisors,
    IReadOnlyList<EducatorHierarchyDto> Subordinates);

public record EducatorListItemDto(
    Guid Id,
    Guid CorporationId,
    string FirstName,
    string LastName,
    string FullName,
    Guid? TitleId,
    string? TitleLabel,
    string? Email,
    string? Phone,
    string? EmploymentType,
    bool IsActive,
    Guid? PrimaryCampusId,
    string? PrimaryCampusName,
    int SpecialtyCount,
    DateTimeOffset CreatedAt);

public record EducatorSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    Guid? TitleId,
    string? TitleLabel,
    bool IsActive,
    string? PrimaryCampusName);

// ── Campus DTOs ───────────────────────────────────────────────────────────────

public record EducatorCampusDto(
    Guid Id,
    Guid CampusId,
    string? CampusName,
    bool IsPrimary,
    DateOnly ActiveFrom,
    DateOnly? ActiveTo,
    bool IsActive);

// ── Specialty DTOs ────────────────────────────────────────────────────────────

public record EducatorSpecialtyDto(
    Guid Id,
    Guid SpecialtyId,
    string? SpecialtyLabel);

// ── Certification DTOs ────────────────────────────────────────────────────────

public record EducatorCertificationDto(
    Guid Id,
    Guid? CertificationTypeId,
    string? CertificationTypeLabel,
    string Name,
    string? Issuer,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    bool IsExpired,
    Guid? FileId,
    DateTimeOffset CreatedAt,
    int RowVersion);

// ── Hierarchy DTOs ────────────────────────────────────────────────────────────

public record EducatorHierarchyDto(
    Guid Id,
    Guid EducatorId,
    string EducatorFullName,
    Guid SupervisorId,
    string SupervisorFullName,
    Guid? RelationshipId,
    string? RelationshipLabel,
    Guid? CampusId,
    string? CampusName,
    DateOnly ActiveFrom,
    DateOnly? ActiveTo,
    bool IsActive);

// ── Availability / Utilization DTOs ──────────────────────────────────────────

public record EducatorAvailabilityDto(
    Guid Id,
    string FullName,
    bool IsActive,
    IReadOnlyList<EducatorCampusDto> ActiveCampuses,
    IReadOnlyList<EducatorSpecialtyDto> Specialties,
    int ActiveStudentProgramCount);

public record EducatorUtilizationDto(
    Guid Id,
    string FullName,
    string? TitleLabel,
    string? PrimaryCampusName,
    int ActiveStudentProgramCount,
    int TotalStudentProgramCount,
    int SpecialtyCount,
    int CertificationCount);

// ── Projection Helper ─────────────────────────────────────────────────────────

internal static class EducatorProjection
{
    public static async Task<EducatorDto?> LoadAsync(
        IAppDbContext db, Guid educatorId, CancellationToken ct)
    {
        var educator = await db.Educators
            .AsNoTracking()
            .Include(e => e.Campuses)
            .Include(e => e.Specialties)
            .Include(e => e.Certifications)
            .Include(e => e.Supervisors)
            .Include(e => e.Subordinates)
            .FirstOrDefaultAsync(e => e.Id == educatorId, ct);

        if (educator is null) return null;

        var titleLabel = educator.TitleId.HasValue
            ? await db.RefValues.AsNoTracking()
                .Where(r => r.Id == educator.TitleId.Value)
                .Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var campusName = educator.PrimaryCampusId.HasValue
            ? await db.Campuses.AsNoTracking()
                .Where(c => c.Id == educator.PrimaryCampusId.Value)
                .Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;

        var campusDtos = await BuildCampusDtosAsync(db, educator.Campuses, ct);
        var specialtyDtos = await BuildSpecialtyDtosAsync(db, educator.Specialties, ct);
        var certDtos = educator.Certifications
            .Where(c => c.DeletedAt == null)
            .Select(ToCertificationDto).ToList();

        var supervisorDtos = await BuildHierarchyDtosAsync(db, educator.Supervisors, ct);
        var subordinateDtos = await BuildHierarchyDtosAsync(db, educator.Subordinates, ct);

        return new EducatorDto(
            educator.Id, educator.CorporationId, educator.UserId,
            educator.FirstName, educator.LastName,
            $"{educator.FirstName} {educator.LastName}",
            educator.TitleId, titleLabel,
            educator.Email, educator.Phone, educator.EmploymentType,
            educator.HireDate, educator.IsActive,
            educator.PrimaryCampusId, campusName,
            educator.RowVersion, educator.CreatedAt, educator.UpdatedAt,
            campusDtos, specialtyDtos, certDtos,
            supervisorDtos, subordinateDtos);
    }

    private static async Task<IReadOnlyList<EducatorCampusDto>> BuildCampusDtosAsync(
        IAppDbContext db, IEnumerable<EducatorCampus> campuses, CancellationToken ct)
    {
        var result = new List<EducatorCampusDto>();
        foreach (var c in campuses)
        {
            var name = await db.Campuses.AsNoTracking()
                .Where(x => x.Id == c.CampusId)
                .Select(x => x.Name).FirstOrDefaultAsync(ct);
            result.Add(new EducatorCampusDto(c.Id, c.CampusId, name, c.IsPrimary,
                c.ActiveFrom, c.ActiveTo, c.IsActive));
        }
        return result;
    }

    private static async Task<IReadOnlyList<EducatorSpecialtyDto>> BuildSpecialtyDtosAsync(
        IAppDbContext db, IEnumerable<EducatorSpecialty> specialties, CancellationToken ct)
    {
        var result = new List<EducatorSpecialtyDto>();
        foreach (var s in specialties)
        {
            var label = await db.RefValues.AsNoTracking()
                .Where(r => r.Id == s.SpecialtyId)
                .Select(r => r.Code).FirstOrDefaultAsync(ct);
            result.Add(new EducatorSpecialtyDto(s.Id, s.SpecialtyId, label));
        }
        return result;
    }

    private static async Task<IReadOnlyList<EducatorHierarchyDto>> BuildHierarchyDtosAsync(
        IAppDbContext db, IEnumerable<EducatorHierarchy> edges, CancellationToken ct)
    {
        var result = new List<EducatorHierarchyDto>();
        foreach (var e in edges)
        {
            var educatorName = await db.Educators.AsNoTracking()
                .Where(x => x.Id == e.EducatorId)
                .Select(x => x.FirstName + " " + x.LastName).FirstOrDefaultAsync(ct) ?? string.Empty;
            var supervisorName = await db.Educators.AsNoTracking()
                .Where(x => x.Id == e.SupervisorId)
                .Select(x => x.FirstName + " " + x.LastName).FirstOrDefaultAsync(ct) ?? string.Empty;
            var relLabel = e.RelationshipId.HasValue
                ? await db.RefValues.AsNoTracking()
                    .Where(r => r.Id == e.RelationshipId.Value)
                    .Select(r => r.Code).FirstOrDefaultAsync(ct)
                : null;
            var campusName = e.CampusId.HasValue
                ? await db.Campuses.AsNoTracking()
                    .Where(c => c.Id == e.CampusId.Value)
                    .Select(c => c.Name).FirstOrDefaultAsync(ct)
                : null;

            result.Add(new EducatorHierarchyDto(
                e.Id, e.EducatorId, educatorName,
                e.SupervisorId, supervisorName,
                e.RelationshipId, relLabel,
                e.CampusId, campusName,
                e.ActiveFrom, e.ActiveTo, e.IsActive));
        }
        return result;
    }

    public static EducatorCertificationDto ToCertificationDto(EducatorCertification c)
        => new(c.Id, c.CertificationTypeId, null, c.Name, c.Issuer,
            c.IssuedOn, c.ExpiresOn, c.IsExpired, c.FileId, c.CreatedAt, c.RowVersion);

    public static EducatorCampusDto ToCampusDto(EducatorCampus c, string? campusName)
        => new(c.Id, c.CampusId, campusName, c.IsPrimary, c.ActiveFrom, c.ActiveTo, c.IsActive);

    public static EducatorSpecialtyDto ToSpecialtyDto(EducatorSpecialty s, string? label)
        => new(s.Id, s.SpecialtyId, label);
}
