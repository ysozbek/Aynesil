using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Commands;
using Aynesil.Application.Features.Consultancy.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Queries;

// ── GetConsultancyAgreementsQuery ─────────────────────────────────────────────

public class GetConsultancyAgreementsQuery
    : PagedQuery, IRequest<PaginatedResult<ConsultancyAgreementListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? ConsultancyPlanId { get; set; }
    public Guid? InstitutionId { get; set; }
    public string? Status { get; set; }
    public Guid? AgreementTypeId { get; set; }
}

public sealed class GetConsultancyAgreementsQueryHandler
    : IRequestHandler<GetConsultancyAgreementsQuery, PaginatedResult<ConsultancyAgreementListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetConsultancyAgreementsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ConsultancyAgreementListItemDto>> Handle(
        GetConsultancyAgreementsQuery req, CancellationToken ct)
    {
        var q = _db.ConsultancyAgreements.AsNoTracking()
            .Where(a => a.DeletedAt == null);

        if (req.CorporationId.HasValue)
            q = q.Where(a => a.CorporationId == req.CorporationId.Value);
        if (req.ConsultancyPlanId.HasValue)
            q = q.Where(a => a.ConsultancyPlanId == req.ConsultancyPlanId.Value);
        if (req.InstitutionId.HasValue)
            q = q.Where(a => a.InstitutionId == req.InstitutionId.Value);
        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(a => a.Status == req.Status);
        if (req.AgreementTypeId.HasValue)
            q = q.Where(a => a.AgreementTypeId == req.AgreementTypeId.Value);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(a => a.Title.ToLower().Contains(term));
        }

        var query =
            from a in q
            join p  in _db.ConsultancyPlans.AsNoTracking() on a.ConsultancyPlanId equals p.Id
            join i  in _db.Institutions.AsNoTracking()      on a.InstitutionId      equals i.Id
            join typ in _db.RefValues.AsNoTracking()
                on a.AgreementTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new ConsultancyAgreementListItemDto(
                a.Id, a.CorporationId,
                a.ConsultancyPlanId, p.Name,
                a.InstitutionId, i.Name,
                a.AgreementTypeId, typ != null ? typ.Code : null,
                a.Title, a.StartDate, a.EndDate, a.SignedDate,
                a.Status, a.FileId != null,
                a.CreatedAt, a.UpdatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "title"       => req.IsDescending ? query.OrderByDescending(x => x.Title)       : query.OrderBy(x => x.Title),
            "status"      => req.IsDescending ? query.OrderByDescending(x => x.Status)      : query.OrderBy(x => x.Status),
            "institution" => req.IsDescending ? query.OrderByDescending(x => x.InstitutionName) : query.OrderBy(x => x.InstitutionName),
            "startdate"   => req.IsDescending ? query.OrderByDescending(x => x.StartDate)   : query.OrderBy(x => x.StartDate),
            _             => query.OrderByDescending(x => x.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<ConsultancyAgreementListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetConsultancyAgreementQuery ──────────────────────────────────────────────

public record GetConsultancyAgreementQuery(Guid Id) : IRequest<ConsultancyAgreementDto>;

public sealed class GetConsultancyAgreementQueryHandler
    : IRequestHandler<GetConsultancyAgreementQuery, ConsultancyAgreementDto>
{
    private readonly IAppDbContext _db;

    public GetConsultancyAgreementQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ConsultancyAgreementDto> Handle(
        GetConsultancyAgreementQuery req, CancellationToken ct)
        => await CreateConsultancyAgreementCommandHandler.ProjectAgreementDto(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Agreement {req.Id} not found.");
}

// ── GetAgreementSummaryQuery ──────────────────────────────────────────────────

/// <summary>Agreement status summary grouped by consultancy plan.</summary>
public class GetAgreementSummaryQuery : IRequest<IReadOnlyList<AgreementSummaryDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? InstitutionId { get; set; }
}

public sealed class GetAgreementSummaryQueryHandler
    : IRequestHandler<GetAgreementSummaryQuery, IReadOnlyList<AgreementSummaryDto>>
{
    private readonly IAppDbContext _db;

    public GetAgreementSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AgreementSummaryDto>> Handle(
        GetAgreementSummaryQuery req, CancellationToken ct)
    {
        var plansQ = _db.ConsultancyPlans.AsNoTracking()
            .Where(p => p.CorporationId == req.CorporationId);

        if (req.InstitutionId.HasValue)
            plansQ = plansQ.Where(p => p.InstitutionId == req.InstitutionId.Value);

        return await (
            from p in plansQ
            join i in _db.Institutions.AsNoTracking() on p.InstitutionId equals i.Id
            select new AgreementSummaryDto(
                p.Id,
                p.Name,
                i.Name,
                p.Agreements.Count(a => a.DeletedAt == null),
                p.Agreements.Count(a => a.Status == "draft"     && a.DeletedAt == null),
                p.Agreements.Count(a => a.Status == "sent"      && a.DeletedAt == null),
                p.Agreements.Count(a => a.Status == "signed"    && a.DeletedAt == null),
                p.Agreements.Count(a => a.Status == "expired"   && a.DeletedAt == null),
                p.Agreements.Count(a => a.Status == "cancelled" && a.DeletedAt == null))
        ).OrderBy(x => x.InstitutionName).ThenBy(x => x.PlanName).ToListAsync(ct);
    }
}
