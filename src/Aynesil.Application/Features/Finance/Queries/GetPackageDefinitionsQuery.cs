using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetPackageDefinitionsQuery ────────────────────────────────────────────────

public class GetPackageDefinitionsQuery : PagedQuery, IRequest<PaginatedResult<PackageDefinitionListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? PackageTypeId { get; set; }
    public Guid? ProgramId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetPackageDefinitionsQueryHandler
    : IRequestHandler<GetPackageDefinitionsQuery, PaginatedResult<PackageDefinitionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetPackageDefinitionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<PackageDefinitionListItemDto>> Handle(
        GetPackageDefinitionsQuery req, CancellationToken ct)
    {
        var q = _db.PackageDefinitions.AsNoTracking();

        if (req.CorporationId.HasValue)  q = q.Where(p => p.CorporationId == req.CorporationId.Value);
        if (req.PackageTypeId.HasValue)  q = q.Where(p => p.PackageTypeId == req.PackageTypeId.Value);
        if (req.ProgramId.HasValue)      q = q.Where(p => p.ProgramId == req.ProgramId.Value);
        if (req.IsActive.HasValue)       q = q.Where(p => p.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(s) || p.Code.ToLower().Contains(s));
        }

        var query = q.Select(p => new PackageDefinitionListItemDto(
            p.Id, p.CorporationId, p.Code, p.Name,
            p.PackageTypeId, p.TotalCredits, p.ListPrice, p.Currency, p.IsActive));

        query = req.SortBy?.ToLower() switch
        {
            "name"      => req.IsDescending ? query.OrderByDescending(p => p.Name)      : query.OrderBy(p => p.Name),
            "listprice" => req.IsDescending ? query.OrderByDescending(p => p.ListPrice)  : query.OrderBy(p => p.ListPrice),
            _           => query.OrderBy(p => p.Name)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<PackageDefinitionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetPackageDefinitionQuery ─────────────────────────────────────────────────

public record GetPackageDefinitionQuery(Guid Id) : IRequest<PackageDefinitionDto>;

public sealed class GetPackageDefinitionQueryHandler
    : IRequestHandler<GetPackageDefinitionQuery, PackageDefinitionDto>
{
    private readonly IAppDbContext _db;

    public GetPackageDefinitionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PackageDefinitionDto> Handle(
        GetPackageDefinitionQuery req, CancellationToken ct)
    {
        var p = await _db.PackageDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Package definition {req.Id} not found.");

        return new PackageDefinitionDto(
            p.Id, p.CorporationId, p.Code, p.Name,
            p.PackageTypeId, p.ProgramId, p.TotalCredits, p.ValidityDays,
            p.ListPrice, p.Currency, p.IsActive,
            p.CreatedAt, p.UpdatedAt, p.RowVersion);
    }
}
