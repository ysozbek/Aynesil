using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Media.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IViewingLogRepository"/>.
/// ViewingLog has a composite bigint+timestamptz PK and is partitioned — it cannot use
/// GenericRepository&lt;T&gt; (which assumes UUID PK + BaseEntity).
/// EF writes to the parent table; PostgreSQL routes to the correct partition automatically.
/// </summary>
internal sealed class ViewingLogRepository : IViewingLogRepository
{
    private readonly AynesilDbContext _context;

    public ViewingLogRepository(AynesilDbContext context) => _context = context;

    public async Task AddAsync(ViewingLog log, CancellationToken ct = default)
    {
        _context.ViewingLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<ViewingLog?> GetOpenAsync(
        long id, DateTimeOffset startedAt, CancellationToken ct = default)
        => await _context.ViewingLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.StartedAt == startedAt && l.EndedAt == null, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
