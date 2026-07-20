using Aynesil.Domain.Modules.Media.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Repository contract for media.viewing_log.
/// ViewingLog uses a composite bigint+timestamptz PK and is partitioned,
/// so it cannot extend IRepository&lt;T&gt; (which requires BaseEntity / UUID PK).
/// </summary>
public interface IViewingLogRepository
{
    /// <summary>Inserts a new viewing log entry. Id is assigned by the database.</summary>
    Task AddAsync(ViewingLog log, CancellationToken ct = default);

    /// <summary>Returns an open (not yet ended) viewing session by its composite PK.</summary>
    Task<ViewingLog?> GetOpenAsync(long id, DateTimeOffset startedAt, CancellationToken ct = default);

    /// <summary>Saves changes to an already-tracked ViewingLog (used to persist End()).</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
