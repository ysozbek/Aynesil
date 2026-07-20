using Aynesil.Domain.Modules.Media.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Repository contract for the Media / Camera bounded context.
/// </summary>
public interface ICameraRepository : IRepository<Camera>
{
    /// <summary>Returns a camera with its room and session assignments.</summary>
    Task<Camera?> GetByIdWithAssignmentsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Checks whether a code already exists in the corporation (excluding the given camera).</summary>
    Task<bool> CodeExistsAsync(
        Guid corporationId,
        string code,
        Guid? excludeId = null,
        CancellationToken ct = default);
}
