using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Queries;

// ── GetContractReportQuery ────────────────────────────────────────────────────

/// <summary>
/// Contract status summary per student.
/// Shows active/expired/terminated contract counts and the date of the latest signature.
/// Useful for compliance tracking (which students are missing an active contract).
/// </summary>
public record GetContractReportQuery(
    Guid CorporationId,
    Guid? StudentId = null,
    string? Status = null) : IRequest<IReadOnlyList<ContractReportItemDto>>;

public sealed class GetContractReportQueryHandler
    : IRequestHandler<GetContractReportQuery, IReadOnlyList<ContractReportItemDto>>
{
    private readonly IAppDbContext _db;

    public GetContractReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContractReportItemDto>> Handle(
        GetContractReportQuery req, CancellationToken ct)
    {
        var query = _db.StudentContracts.AsNoTracking()
            .Where(c => c.CorporationId == req.CorporationId);

        if (req.StudentId.HasValue) query = query.Where(c => c.StudentId == req.StudentId);

        var grouped = await query
            .GroupBy(c => c.StudentId)
            .Select(g => new
            {
                StudentId          = g.Key,
                Total              = g.Count(),
                DraftCount         = g.Count(c => c.Status == "draft"),
                ActiveCount        = g.Count(c => c.Status == "active"),
                ExpiredCount       = g.Count(c => c.Status == "expired"),
                TerminatedCount    = g.Count(c => c.Status == "terminated"),
                LatestSignedAt     = g.Max(c => c.SignedAt)
            })
            .ToListAsync(ct);

        var studentIds = grouped.Select(g => g.StudentId).Distinct().ToList();
        var studentNames = studentIds.Count > 0
            ? await _db.Students.AsNoTracking()
                .Where(s => studentIds.Contains(s.Id))
                .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct)
            : new Dictionary<Guid, string>();

        return grouped.Select(g => new ContractReportItemDto(
            g.StudentId,
            studentNames.GetValueOrDefault(g.StudentId, string.Empty),
            g.Total, g.DraftCount, g.ActiveCount, g.ExpiredCount, g.TerminatedCount,
            g.LatestSignedAt)).ToList();
    }
}

// ── GetConsentReportQuery ─────────────────────────────────────────────────────

/// <summary>
/// Consent compliance report: one row per student per consent type.
/// Shows whether the latest consent record is granted or withdrawn.
/// Mandatory consent types are flagged, enabling compliance gap detection.
/// </summary>
public record GetConsentReportQuery(
    Guid CorporationId,
    Guid? StudentId = null,
    Guid? ConsentTypeId = null,
    bool IncludeExpired = false) : IRequest<IReadOnlyList<ConsentReportItemDto>>;

public sealed class GetConsentReportQueryHandler
    : IRequestHandler<GetConsentReportQuery, IReadOnlyList<ConsentReportItemDto>>
{
    private readonly IAppDbContext _db;

    public GetConsentReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConsentReportItemDto>> Handle(
        GetConsentReportQuery req, CancellationToken ct)
    {
        var baseQuery = req.IncludeExpired
            ? _db.StudentConsents.AsNoTracking().IgnoreQueryFilters()
              .Where(c => c.CorporationId == req.CorporationId)
            : _db.StudentConsents.AsNoTracking()
              .Where(c => c.CorporationId == req.CorporationId);

        if (req.StudentId.HasValue)     baseQuery = baseQuery.Where(c => c.StudentId == req.StudentId);
        if (req.ConsentTypeId.HasValue) baseQuery = baseQuery.Where(c => c.ConsentTypeId == req.ConsentTypeId);

        // Latest row per (student, consent_type) — EF grouped projection.
        var latest = await baseQuery
            .GroupBy(c => new { c.StudentId, c.ConsentTypeId })
            .Select(g => new
            {
                g.Key.StudentId,
                g.Key.ConsentTypeId,
                State       = g.OrderByDescending(c => c.CreatedAt).First().State,
                GrantedAt   = g.OrderByDescending(c => c.CreatedAt).First().GrantedAt,
                WithdrawnAt = g.OrderByDescending(c => c.CreatedAt).First().WithdrawnAt,
                ValidUntil  = g.OrderByDescending(c => c.CreatedAt).First().ValidUntil
            })
            .ToListAsync(ct);

        var studentIds = latest.Select(l => l.StudentId).Distinct().ToList();
        var typeIds    = latest.Where(l => l.ConsentTypeId.HasValue)
            .Select(l => l.ConsentTypeId!.Value).Distinct().ToList();

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

        // Resolve mandatory flag from current consent templates.
        var mandatoryTypeIds = new HashSet<Guid>(
            await _db.ConsentTemplates.AsNoTracking()
                .Where(t => t.IsMandatory && t.IsCurrent && t.CorporationId == req.CorporationId)
                .Select(t => t.ConsentTypeId!.Value)
                .ToListAsync(ct));

        return latest.Select(l => new ConsentReportItemDto(
            l.StudentId,
            studentNames.GetValueOrDefault(l.StudentId, string.Empty),
            l.ConsentTypeId,
            l.ConsentTypeId.HasValue ? typeCodes.GetValueOrDefault(l.ConsentTypeId.Value) : null,
            l.State == "granted",
            l.GrantedAt, l.WithdrawnAt, l.ValidUntil,
            l.ConsentTypeId.HasValue && mandatoryTypeIds.Contains(l.ConsentTypeId.Value))).ToList();
    }
}

// ── GetSignatureReportQuery ───────────────────────────────────────────────────

/// <summary>
/// Digital-signature readiness report: lists student contracts with their signature
/// status, method and whether a signed file is attached.
/// Supports audit and compliance (e.g. identifying contracts with no signed PDF).
/// </summary>
public record GetSignatureReportQuery(
    Guid CorporationId,
    Guid? StudentId = null,
    string? Status = null,
    string? SignatureMethod = null) : IRequest<IReadOnlyList<SignatureReportItemDto>>;

public sealed class GetSignatureReportQueryHandler
    : IRequestHandler<GetSignatureReportQuery, IReadOnlyList<SignatureReportItemDto>>
{
    private readonly IAppDbContext _db;

    public GetSignatureReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SignatureReportItemDto>> Handle(
        GetSignatureReportQuery req, CancellationToken ct)
    {
        var query = _db.StudentContracts.AsNoTracking()
            .Where(c => c.CorporationId == req.CorporationId);

        if (req.StudentId.HasValue) query = query.Where(c => c.StudentId == req.StudentId);
        if (!string.IsNullOrEmpty(req.Status)) query = query.Where(c => c.Status == req.Status);
        if (!string.IsNullOrEmpty(req.SignatureMethod))
            query = query.Where(c => c.SignatureMethod == req.SignatureMethod);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var studentIds = items.Select(c => c.StudentId).Distinct().ToList();
        var studentNames = studentIds.Count > 0
            ? await _db.Students.AsNoTracking()
                .Where(s => studentIds.Contains(s.Id))
                .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct)
            : new Dictionary<Guid, string>();

        return items.Select(c => new SignatureReportItemDto(
            c.Id, c.StudentId,
            studentNames.GetValueOrDefault(c.StudentId, string.Empty),
            c.Status, c.SignatureMethod, c.SignatureRef,
            c.SignedFileId.HasValue, c.SignedAt, c.SignedByName)).ToList();
    }
}
