using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Queries;

// ── GetStudentStatusHistoryQuery ──────────────────────────────────────────────

public record GetStudentStatusHistoryQuery(Guid StudentId)
    : IRequest<IReadOnlyList<StudentStatusHistoryDto>>;

public sealed class GetStudentStatusHistoryQueryHandler
    : IRequestHandler<GetStudentStatusHistoryQuery, IReadOnlyList<StudentStatusHistoryDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentStatusHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StudentStatusHistoryDto>> Handle(
        GetStudentStatusHistoryQuery req, CancellationToken ct)
    {
        var items = await (
            from h in _db.StudentStatusHistories.AsNoTracking()
            join status in _db.RefValues.AsNoTracking()
                on h.StatusId equals status.Id into statusGrp
            from status in statusGrp.DefaultIfEmpty()
            where h.StudentId == req.StudentId
            orderby h.ChangedAt descending
            select new StudentStatusHistoryDto(
                h.Id, h.StatusId,
                status != null ? status.Code : null,
                h.Reason, h.ChangedAt, h.ChangedBy)
        ).ToListAsync(ct);

        return items;
    }
}

// ── GetStudentCampusesQuery ───────────────────────────────────────────────────

public record GetStudentCampusesQuery(Guid StudentId, bool ActiveOnly = false)
    : IRequest<IReadOnlyList<StudentCampusDto>>;

public sealed class GetStudentCampusesQueryHandler
    : IRequestHandler<GetStudentCampusesQuery, IReadOnlyList<StudentCampusDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentCampusesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StudentCampusDto>> Handle(
        GetStudentCampusesQuery req, CancellationToken ct)
    {
        var query = _db.StudentCampuses
            .AsNoTracking()
            .Where(sc => sc.StudentId == req.StudentId);

        if (req.ActiveOnly)
            query = query.Where(sc => sc.ActiveTo == null);

        var campuses = await query.ToListAsync(ct);

        var result = new List<StudentCampusDto>();
        foreach (var sc in campuses)
        {
            var campusName = await _db.Campuses
                .AsNoTracking()
                .Where(c => c.Id == sc.CampusId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);

            result.Add(new StudentCampusDto(
                sc.Id, sc.CampusId, campusName, sc.IsPrimary,
                sc.ActiveFrom, sc.ActiveTo, sc.IsActive));
        }

        return result;
    }
}

// ── GetDiagnosesQuery ─────────────────────────────────────────────────────────

public record GetDiagnosesQuery(Guid StudentId) : IRequest<IReadOnlyList<DiagnosisDto>>;

public sealed class GetDiagnosesQueryHandler
    : IRequestHandler<GetDiagnosesQuery, IReadOnlyList<DiagnosisDto>>
{
    private readonly IAppDbContext _db;

    public GetDiagnosesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DiagnosisDto>> Handle(GetDiagnosesQuery req, CancellationToken ct)
    {
        return await (
            from d in _db.Diagnoses.AsNoTracking()
            join cat in _db.RefValues.AsNoTracking()
                on d.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            where d.StudentId == req.StudentId
            orderby d.DiagnosedOn descending
            select new DiagnosisDto(
                d.Id, d.StudentId, d.CategoryId,
                cat != null ? cat.Code : null,
                d.IcdCode, d.Description, d.DiagnosedOn, d.DiagnosedBy,
                d.SourceFileId, d.CreatedAt, d.RowVersion)
        ).ToListAsync(ct);
    }
}

// ── GetDevelopmentalProfilesQuery ─────────────────────────────────────────────

public record GetDevelopmentalProfilesQuery(Guid StudentId)
    : IRequest<IReadOnlyList<DevelopmentalProfileDto>>;

public sealed class GetDevelopmentalProfilesQueryHandler
    : IRequestHandler<GetDevelopmentalProfilesQuery, IReadOnlyList<DevelopmentalProfileDto>>
{
    private readonly IAppDbContext _db;

    public GetDevelopmentalProfilesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DevelopmentalProfileDto>> Handle(
        GetDevelopmentalProfilesQuery req, CancellationToken ct)
    {
        return await (
            from p in _db.DevelopmentalProfiles.AsNoTracking()
            join area in _db.RefValues.AsNoTracking()
                on p.DevelopmentAreaId equals area.Id into areaGrp
            from area in areaGrp.DefaultIfEmpty()
            where p.StudentId == req.StudentId
            select new DevelopmentalProfileDto(
                p.Id, p.DevelopmentAreaId,
                area != null ? area.Code : null,
                p.Summary, p.Strengths, p.Needs, p.AssessedOn,
                p.CreatedAt, p.UpdatedAt, p.RowVersion)
        ).ToListAsync(ct);
    }
}

// ── GetCaseNotesQuery ─────────────────────────────────────────────────────────

public class GetCaseNotesQuery : PagedQuery, IRequest<PaginatedResult<CaseNoteDto>>
{
    public Guid StudentId { get; set; }
    /// <summary>When false, confidential notes are excluded. Pass true only for clinical staff.</summary>
    public bool IncludeConfidential { get; set; }
    public string? NoteType { get; set; }
}

public sealed class GetCaseNotesQueryHandler
    : IRequestHandler<GetCaseNotesQuery, PaginatedResult<CaseNoteDto>>
{
    private readonly IAppDbContext _db;

    public GetCaseNotesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<CaseNoteDto>> Handle(GetCaseNotesQuery req, CancellationToken ct)
    {
        var query = _db.CaseNotes
            .AsNoTracking()
            .Where(n => n.StudentId == req.StudentId);

        if (!req.IncludeConfidential)
            query = query.Where(n => !n.IsConfidential);

        if (!string.IsNullOrWhiteSpace(req.NoteType))
            query = query.Where(n => n.NoteType == req.NoteType);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(n => n.Body.ToLower().Contains(req.Search.ToLower()));

        var ordered = query.OrderByDescending(n => n.CreatedAt);

        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip(req.Skip).Take(req.PageSize)
            .Select(n => new CaseNoteDto(
                n.Id, n.StudentId, n.NoteType, n.Body, n.IsConfidential,
                n.AuthoredBy, n.CreatedAt, n.UpdatedAt, n.RowVersion))
            .ToListAsync(ct);

        return PaginatedResult<CaseNoteDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetMedicalReportsQuery ────────────────────────────────────────────────────

public record GetMedicalReportsQuery(Guid StudentId) : IRequest<IReadOnlyList<MedicalReportDto>>;

public sealed class GetMedicalReportsQueryHandler
    : IRequestHandler<GetMedicalReportsQuery, IReadOnlyList<MedicalReportDto>>
{
    private readonly IAppDbContext _db;

    public GetMedicalReportsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MedicalReportDto>> Handle(GetMedicalReportsQuery req, CancellationToken ct)
        => await _db.MedicalReports.AsNoTracking()
            .Where(r => r.StudentId == req.StudentId)
            .OrderByDescending(r => r.ReportDate)
            .Select(r => new MedicalReportDto(
                r.Id, r.StudentId, r.Title, r.ReportDate, r.Issuer,
                r.Summary, r.FileId, r.CreatedAt, r.RowVersion))
            .ToListAsync(ct);
}

// ── GetDevelopmentReportsQuery ────────────────────────────────────────────────

public record GetDevelopmentReportsQuery(Guid StudentId)
    : IRequest<IReadOnlyList<DevelopmentReportDto>>;

public sealed class GetDevelopmentReportsQueryHandler
    : IRequestHandler<GetDevelopmentReportsQuery, IReadOnlyList<DevelopmentReportDto>>
{
    private readonly IAppDbContext _db;

    public GetDevelopmentReportsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DevelopmentReportDto>> Handle(
        GetDevelopmentReportsQuery req, CancellationToken ct)
        => await _db.DevelopmentReports.AsNoTracking()
            .Where(r => r.StudentId == req.StudentId)
            .OrderByDescending(r => r.ReportDate)
            .Select(r => new DevelopmentReportDto(
                r.Id, r.StudentId, r.PeriodLabel, r.ReportDate,
                r.AuthoredBy, r.Content, r.FileId, r.CreatedAt, r.RowVersion))
            .ToListAsync(ct);
}

// ── GetExternalInstitutionReportsQuery ────────────────────────────────────────

public record GetExternalInstitutionReportsQuery(Guid StudentId)
    : IRequest<IReadOnlyList<ExternalInstitutionReportDto>>;

public sealed class GetExternalInstitutionReportsQueryHandler
    : IRequestHandler<GetExternalInstitutionReportsQuery, IReadOnlyList<ExternalInstitutionReportDto>>
{
    private readonly IAppDbContext _db;

    public GetExternalInstitutionReportsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ExternalInstitutionReportDto>> Handle(
        GetExternalInstitutionReportsQuery req, CancellationToken ct)
    {
        return await (
            from r in _db.ExternalInstitutionReports.AsNoTracking()
            join it in _db.RefValues.AsNoTracking()
                on r.InstitutionTypeId equals it.Id into itGrp
            from it in itGrp.DefaultIfEmpty()
            where r.StudentId == req.StudentId
            orderby r.ReportDate descending
            select new ExternalInstitutionReportDto(
                r.Id, r.StudentId, r.InstitutionName, r.InstitutionTypeId,
                it != null ? it.Code : null,
                r.ReportDate, r.Summary, r.FileId, r.CreatedAt, r.RowVersion)
        ).ToListAsync(ct);
    }
}
