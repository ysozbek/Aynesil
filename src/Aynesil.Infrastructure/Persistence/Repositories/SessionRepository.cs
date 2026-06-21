using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISessionRepository"/>.
/// All queries run within the active RLS tenant context and respect the soft-delete filter.
/// Conflict detection queries explicitly filter out cancelled sessions to match the DB
/// EXCLUDE USING GIST constraint semantics.
/// </summary>
internal sealed class SessionRepository : GenericRepository<Session>, ISessionRepository
{
    public SessionRepository(AynesilDbContext context) : base(context) { }

    public async Task<Session?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(s => s.Room)
            .Include(s => s.Participants)
            .Include(s => s.Educators)
            .Include(s => s.Goals)
            .Include(s => s.Notes.Where(n => n.DeletedAt == null))
            .Include(s => s.Attendances)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Session>> GetByCampusAsync(
        Guid corporationId,
        Guid campusId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
        => await Set
            .Where(s => s.CorporationId == corporationId
                     && s.CampusId == campusId
                     && s.StartsAt < to
                     && s.EndsAt > from)
            .Include(s => s.Participants)
            .Include(s => s.Educators)
            .OrderBy(s => s.StartsAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Session>> GetByRoomAsync(
        Guid roomId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
        => await Set
            .Where(s => s.RoomId == roomId
                     && s.StartsAt < to
                     && s.EndsAt > from)
            .OrderBy(s => s.StartsAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Session>> GetByStudentAsync(
        Guid corporationId,
        Guid studentId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
        => await Set
            .Where(s => s.CorporationId == corporationId
                     && s.Participants.Any(p => p.StudentId == studentId)
                     && s.StartsAt < to
                     && s.EndsAt > from)
            .Include(s => s.Room)
            .Include(s => s.Educators)
            .OrderBy(s => s.StartsAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Session>> GetByEducatorAsync(
        Guid corporationId,
        Guid educatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
        => await Set
            .Where(s => s.CorporationId == corporationId
                     && s.Educators.Any(e => e.EducatorId == educatorId)
                     && s.StartsAt < to
                     && s.EndsAt > from)
            .Include(s => s.Room)
            .Include(s => s.Participants)
            .OrderBy(s => s.StartsAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<bool> HasRoomConflictAsync(
        Guid roomId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid? excludeSessionId = null,
        CancellationToken ct = default)
        => await Set
            .Where(s => s.RoomId == roomId
                     && s.Status != "cancelled"
                     && s.DeletedAt == null
                     && s.StartsAt < endsAt
                     && s.EndsAt > startsAt
                     && (excludeSessionId == null || s.Id != excludeSessionId.Value))
            .AnyAsync(ct);

    public async Task<bool> HasEducatorConflictAsync(
        Guid educatorId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid? excludeSessionId = null,
        CancellationToken ct = default)
        => await Context.Set<SessionEducator>()
            .Join(Set,
                se => se.SessionId,
                s  => s.Id,
                (se, s) => new { se, s })
            .Where(x => x.se.EducatorId == educatorId
                      && x.s.Status != "cancelled"
                      && x.s.DeletedAt == null
                      && x.s.StartsAt < endsAt
                      && x.s.EndsAt > startsAt
                      && (excludeSessionId == null || x.s.Id != excludeSessionId.Value))
            .AnyAsync(ct);
}
