using Aynesil.Domain.Modules.Education.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Repository contract for the Education (Programs) bounded context.
/// Read queries that require complex projections or cross-table JOINs should bypass
/// this interface and use IAppDbContext directly in query handlers.
/// </summary>
public interface IProgramRepository : IRepository<EducationProgram>
{
    /// <summary>Returns the program with its services and translations.</summary>
    Task<EducationProgram?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns active programs for a corporation, optionally filtered by type.</summary>
    Task<IReadOnlyList<EducationProgram>> GetByCorporationAsync(
        Guid corporationId,
        Guid? programTypeId = null,
        bool activeOnly = true,
        CancellationToken ct = default);

    /// <summary>Checks whether a program code is already taken within the corporation.</summary>
    Task<bool> CodeExistsAsync(
        Guid corporationId,
        string code,
        Guid? excludeId = null,
        CancellationToken ct = default);
}
