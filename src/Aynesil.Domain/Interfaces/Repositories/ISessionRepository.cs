using Aynesil.Domain.Modules.Scheduling.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Repository contract for the Scheduling bounded context.
/// Complex projections (calendar views, analytics) bypass this and use IAppDbContext directly.
/// </summary>
public interface ISessionRepository : IRepository<Session>
{
    /// <summary>Returns a session with all child collections (participants, educators, goals, notes, attendance).</summary>
    Task<Session?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns sessions for a campus within a date range, ordered by start time.</summary>
    Task<IReadOnlyList<Session>> GetByCampusAsync(
        Guid corporationId,
        Guid campusId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);

    /// <summary>Returns sessions for a room within a date range.</summary>
    Task<IReadOnlyList<Session>> GetByRoomAsync(
        Guid roomId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);

    /// <summary>Returns sessions for a student within a date range.</summary>
    Task<IReadOnlyList<Session>> GetByStudentAsync(
        Guid corporationId,
        Guid studentId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);

    /// <summary>Returns sessions for an educator within a date range.</summary>
    Task<IReadOnlyList<Session>> GetByEducatorAsync(
        Guid corporationId,
        Guid educatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether a room is already booked during the specified range.
    /// Used as an application-level pre-check before the DB EXCLUDE constraint fires.
    /// </summary>
    Task<bool> HasRoomConflictAsync(
        Guid roomId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid? excludeSessionId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether an educator is double-booked during the specified range.
    /// </summary>
    Task<bool> HasEducatorConflictAsync(
        Guid educatorId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid? excludeSessionId = null,
        CancellationToken ct = default);
}
