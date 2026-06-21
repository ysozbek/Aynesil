using Aynesil.Domain.Modules.Educators.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Repository contract for the Educators bounded context.
/// Read queries that require complex projections or cross-table JOINs should bypass
/// this interface and use IAppDbContext directly in query handlers.
/// </summary>
public interface IEducatorRepository : IRepository<Educator>
{
    /// <summary>
    /// Returns the educator with all sub-records loaded:
    /// campuses, specialties, certifications, hierarchy edges.
    /// </summary>
    Task<Educator?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns all active educators for a given corporation, optionally filtered by campus.
    /// </summary>
    Task<IReadOnlyList<Educator>> GetByCorporationAsync(
        Guid corporationId,
        Guid? campusId = null,
        bool activeOnly = true,
        CancellationToken ct = default);

    /// <summary>
    /// Returns educators who hold a given specialty.
    /// Used for educator availability / matching queries.
    /// </summary>
    Task<IReadOnlyList<Educator>> GetBySpecialtyAsync(
        Guid corporationId,
        Guid specialtyId,
        CancellationToken ct = default);
}
