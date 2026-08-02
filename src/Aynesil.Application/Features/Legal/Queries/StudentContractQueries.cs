using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Queries;

// ── GetStudentContractsQuery ──────────────────────────────────────────────────

public record GetStudentContractsQuery(
    Guid CorporationId,
    Guid? StudentId = null,
    Guid? GuardianId = null,
    string? Status = null,
    Guid? TemplateId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<StudentContractListItemDto>>;

public sealed class GetStudentContractsQueryHandler
    : IRequestHandler<GetStudentContractsQuery, PaginatedResult<StudentContractListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentContractsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<StudentContractListItemDto>> Handle(
        GetStudentContractsQuery req, CancellationToken ct)
    {
        var query = _db.StudentContracts
            .AsNoTracking()
            .Where(c => c.CorporationId == req.CorporationId);

        if (req.StudentId.HasValue)   query = query.Where(c => c.StudentId == req.StudentId);
        if (req.GuardianId.HasValue)  query = query.Where(c => c.GuardianId == req.GuardianId);
        if (!string.IsNullOrEmpty(req.Status)) query = query.Where(c => c.Status == req.Status);
        if (req.TemplateId.HasValue)  query = query.Where(c => c.TemplateId == req.TemplateId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var studentIds = items.Select(c => c.StudentId).Distinct().ToList();
        var templateIds = items.Where(c => c.TemplateId.HasValue)
            .Select(c => c.TemplateId!.Value).Distinct().ToList();

        var studentNames = studentIds.Count > 0
            ? await _db.Students.AsNoTracking()
                .Where(s => studentIds.Contains(s.Id))
                .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct)
            : new Dictionary<Guid, string>();

        var templateCodes = templateIds.Count > 0
            ? await _db.ContractTemplates.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(t => templateIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Code })
                .ToDictionaryAsync(t => t.Id, t => t.Code, ct)
            : new Dictionary<Guid, string>();

        var results = items.Select(c => new StudentContractListItemDto(
            c.Id, c.CorporationId, c.StudentId,
            studentNames.GetValueOrDefault(c.StudentId),
            c.TemplateId, c.TemplateId.HasValue ? templateCodes.GetValueOrDefault(c.TemplateId.Value) : null,
            c.TemplateVersion, c.GuardianId, c.Status,
            c.SignedAt, c.SignatureMethod, c.StartsOn, c.EndsOn, c.CreatedAt)).ToList();

        return PaginatedResult<StudentContractListItemDto>.Create(results, total, req.Page, req.PageSize);
    }
}

// ── GetStudentContractQuery ───────────────────────────────────────────────────

public record GetStudentContractQuery(Guid Id) : IRequest<StudentContractDto>;

public sealed class GetStudentContractQueryHandler
    : IRequestHandler<GetStudentContractQuery, StudentContractDto>
{
    private readonly IAppDbContext _db;

    public GetStudentContractQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StudentContractDto> Handle(GetStudentContractQuery req, CancellationToken ct)
    {
        var c = await _db.StudentContracts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student contract {req.Id} not found.");

        var studentName = await _db.Students.AsNoTracking()
            .Where(s => s.Id == c.StudentId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync(ct);

        string? templateCode = null;
        if (c.TemplateId.HasValue)
            templateCode = await _db.ContractTemplates.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(t => t.Id == c.TemplateId.Value)
                .Select(t => t.Code)
                .FirstOrDefaultAsync(ct);

        return new StudentContractDto(
            c.Id, c.CorporationId, c.StudentId, studentName,
            c.TemplateId, templateCode, c.TemplateVersion,
            c.GuardianId, c.Status,
            c.SignedAt, c.SignedByName, c.SignatureMethod, c.SignatureRef, c.SignedFileId,
            c.StartsOn, c.EndsOn, c.CreatedAt, c.CreatedBy, c.UpdatedAt, c.RowVersion);
    }
}
