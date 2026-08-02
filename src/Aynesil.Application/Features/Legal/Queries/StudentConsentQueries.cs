using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Queries;

// ── GetStudentConsentsQuery ───────────────────────────────────────────────────

public record GetStudentConsentsQuery(
    Guid CorporationId,
    Guid? StudentId = null,
    Guid? ConsentTypeId = null,
    string? State = null,
    bool IncludeExpired = false,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<StudentConsentListItemDto>>;

public sealed class GetStudentConsentsQueryHandler
    : IRequestHandler<GetStudentConsentsQuery, PaginatedResult<StudentConsentListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentConsentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<StudentConsentListItemDto>> Handle(
        GetStudentConsentsQuery req, CancellationToken ct)
    {
        // Base query — optionally include expired via IgnoreQueryFilters.
        var baseQuery = req.IncludeExpired
            ? _db.StudentConsents.AsNoTracking().IgnoreQueryFilters()
              .Where(c => c.CorporationId == req.CorporationId)
            : _db.StudentConsents.AsNoTracking()
              .Where(c => c.CorporationId == req.CorporationId);

        if (req.StudentId.HasValue)      baseQuery = baseQuery.Where(c => c.StudentId == req.StudentId);
        if (req.ConsentTypeId.HasValue)  baseQuery = baseQuery.Where(c => c.ConsentTypeId == req.ConsentTypeId);
        if (!string.IsNullOrEmpty(req.State)) baseQuery = baseQuery.Where(c => c.State == req.State);

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(c => c.GrantedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var studentIds = items.Select(c => c.StudentId).Distinct().ToList();
        var typeIds    = items.Where(c => c.ConsentTypeId.HasValue)
            .Select(c => c.ConsentTypeId!.Value).Distinct().ToList();
        var templateIds = items.Where(c => c.TemplateId.HasValue)
            .Select(c => c.TemplateId!.Value).Distinct().ToList();

        var studentNames = studentIds.Count > 0
            ? await _db.Students.AsNoTracking()
                .Where(s => studentIds.Contains(s.Id))
                .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct)
            : new Dictionary<Guid, string>();

        var typeCodes = typeIds.Count > 0
            ? await _db.RefValues.AsNoTracking()
                .Where(r => typeIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Code })
                .ToDictionaryAsync(r => r.Id, r => r.Code, ct)
            : new Dictionary<Guid, string>();

        var templateCodes = templateIds.Count > 0
            ? await _db.ConsentTemplates.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(t => templateIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Code })
                .ToDictionaryAsync(t => t.Id, t => t.Code, ct)
            : new Dictionary<Guid, string>();

        var results = items.Select(c => new StudentConsentListItemDto(
            c.Id, c.CorporationId, c.StudentId,
            studentNames.GetValueOrDefault(c.StudentId),
            c.ConsentTypeId,
            c.ConsentTypeId.HasValue ? typeCodes.GetValueOrDefault(c.ConsentTypeId.Value) : null,
            c.TemplateId,
            c.TemplateId.HasValue ? templateCodes.GetValueOrDefault(c.TemplateId.Value) : null,
            c.TemplateVersion, c.GuardianId, c.State,
            c.GrantedAt, c.WithdrawnAt, c.ValidUntil,
            c.EvidenceFileId.HasValue, c.CreatedAt)).ToList();

        return PaginatedResult<StudentConsentListItemDto>.Create(results, total, req.Page, req.PageSize);
    }
}

// ── GetStudentConsentQuery ────────────────────────────────────────────────────

public record GetStudentConsentQuery(Guid Id) : IRequest<StudentConsentDto>;

public sealed class GetStudentConsentQueryHandler
    : IRequestHandler<GetStudentConsentQuery, StudentConsentDto>
{
    private readonly IAppDbContext _db;

    public GetStudentConsentQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StudentConsentDto> Handle(GetStudentConsentQuery req, CancellationToken ct)
    {
        var c = await _db.StudentConsents.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student consent {req.Id} not found.");

        var studentName = await _db.Students.AsNoTracking()
            .Where(s => s.Id == c.StudentId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync(ct);

        string? typeCode = null;
        if (c.ConsentTypeId.HasValue)
            typeCode = await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == c.ConsentTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct);

        string? templateCode = null;
        if (c.TemplateId.HasValue)
            templateCode = await _db.ConsentTemplates.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(t => t.Id == c.TemplateId.Value)
                .Select(t => t.Code)
                .FirstOrDefaultAsync(ct);

        return new StudentConsentDto(
            c.Id, c.CorporationId, c.StudentId, studentName,
            c.ConsentTypeId, typeCode,
            c.TemplateId, templateCode, c.TemplateVersion,
            c.GuardianId, c.State,
            c.GrantedAt, c.WithdrawnAt, c.ValidUntil, c.EvidenceFileId,
            c.CreatedAt, c.CreatedBy, c.UpdatedAt, c.RowVersion);
    }
}
