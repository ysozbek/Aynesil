using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Queries;

// ── GetStudentPackagesQuery ───────────────────────────────────────────────────

public class GetStudentPackagesQuery : PagedQuery, IRequest<PaginatedResult<StudentPackageListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? PackageDefinitionId { get; set; }
    public string? Status { get; set; }
    public bool? ExpiringWithin30Days { get; set; }
}

public sealed class GetStudentPackagesQueryHandler
    : IRequestHandler<GetStudentPackagesQuery, PaginatedResult<StudentPackageListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetStudentPackagesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<StudentPackageListItemDto>> Handle(
        GetStudentPackagesQuery req, CancellationToken ct)
    {
        var q = _db.StudentPackages.AsNoTracking();

        if (req.CorporationId.HasValue)      q = q.Where(p => p.CorporationId == req.CorporationId.Value);
        if (req.StudentId.HasValue)          q = q.Where(p => p.StudentId == req.StudentId.Value);
        if (req.PackageDefinitionId.HasValue) q = q.Where(p => p.PackageDefinitionId == req.PackageDefinitionId.Value);
        if (req.Status is not null)          q = q.Where(p => p.Status == req.Status);

        if (req.ExpiringWithin30Days == true)
        {
            var threshold = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
            q = q.Where(p => p.Status == "active" && p.ExpiresOn != null && p.ExpiresOn <= threshold);
        }

        var query =
            from pkg in q
            join s in _db.Students.AsNoTracking()
                on pkg.StudentId equals s.Id
            join def in _db.PackageDefinitions.AsNoTracking()
                on pkg.PackageDefinitionId equals def.Id into defGrp
            from def in defGrp.DefaultIfEmpty()
            select new
            {
                pkg,
                StudentFullName       = s.FirstName + " " + s.LastName,
                PackageDefinitionName = def != null ? def.Name : null,
                RemainingCredits      = _db.CreditLedgerEntries
                    .Where(l => l.StudentPackageId == pkg.Id)
                    .Sum(l => (decimal?)l.Delta) ?? 0m
            };

        var sorted = req.SortBy?.ToLower() switch
        {
            "purchasedon"  => req.IsDescending
                ? query.OrderByDescending(x => x.pkg.PurchasedOn)
                : query.OrderBy(x => x.pkg.PurchasedOn),
            "expireson"    => req.IsDescending
                ? query.OrderByDescending(x => x.pkg.ExpiresOn)
                : query.OrderBy(x => x.pkg.ExpiresOn),
            "studentname"  => req.IsDescending
                ? query.OrderByDescending(x => x.StudentFullName)
                : query.OrderBy(x => x.StudentFullName),
            _              => query.OrderByDescending(x => x.pkg.PurchasedOn)
        };

        var total = await sorted.CountAsync(ct);
        var page  = await sorted.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        var items = page.Select(x => new StudentPackageListItemDto(
            x.pkg.Id, x.pkg.StudentId, x.StudentFullName.Trim(),
            x.pkg.PackageDefinitionId, x.PackageDefinitionName,
            x.pkg.PurchasedOn, x.pkg.ExpiresOn,
            x.pkg.TotalCredits, x.RemainingCredits, x.pkg.Status)).ToList();

        return PaginatedResult<StudentPackageListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetStudentPackageQuery ────────────────────────────────────────────────────

public record GetStudentPackageQuery(Guid Id) : IRequest<StudentPackageDto>;

public sealed class GetStudentPackageQueryHandler
    : IRequestHandler<GetStudentPackageQuery, StudentPackageDto>
{
    private readonly IAppDbContext _db;

    public GetStudentPackageQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StudentPackageDto> Handle(GetStudentPackageQuery req, CancellationToken ct)
        => await FinanceProjection.LoadStudentPackageAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Student package {req.Id} not found.");
}

// ── GetPackageBalanceQuery ────────────────────────────────────────────────────

public record GetPackageBalanceQuery(Guid StudentPackageId) : IRequest<PackageBalanceDto>;

public sealed class GetPackageBalanceQueryHandler
    : IRequestHandler<GetPackageBalanceQuery, PackageBalanceDto>
{
    private readonly IAppDbContext _db;

    public GetPackageBalanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PackageBalanceDto> Handle(GetPackageBalanceQuery req, CancellationToken ct)
    {
        var pkg = await _db.StudentPackages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == req.StudentPackageId, ct)
            ?? throw new KeyNotFoundException($"Student package {req.StudentPackageId} not found.");

        var entries = await _db.CreditLedgerEntries.AsNoTracking()
            .Where(e => e.StudentPackageId == req.StudentPackageId)
            .ToListAsync(ct);

        var remaining = entries.Sum(e => e.Delta);
        var consumed  = entries.Where(e => e.EntryType == "consume").Sum(e => Math.Abs(e.Delta));

        return new PackageBalanceDto(
            pkg.Id, pkg.StudentId,
            pkg.TotalCredits, remaining, consumed,
            pkg.ExpiresOn, pkg.Status);
    }
}
