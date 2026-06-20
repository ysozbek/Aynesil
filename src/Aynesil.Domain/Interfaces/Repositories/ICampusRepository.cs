using Aynesil.Domain.Modules.Core.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Typed repository for <see cref="Campus"/> (branch) aggregate.
/// Extends the generic repository with campus-specific query operations.
/// Campus is an authorization sub-scope within a corporation; RLS enforces tenant isolation at DB level.
/// </summary>
public interface ICampusRepository : IRepository<Campus>
{
    /// <summary>Returns a campus by its machine code within a corporation.</summary>
    Task<Campus?> GetByCodeAsync(Guid corporationId, string code, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the given code is already in use within the corporation.
    /// Pass <paramref name="excludeId"/> to allow the current campus to keep its code during updates.
    /// </summary>
    Task<bool> IsCodeTakenAsync(Guid corporationId, string code, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Returns all campuses for a corporation.
    /// When <paramref name="isActive"/> is provided the result is filtered by active status.
    /// </summary>
    Task<IReadOnlyList<Campus>> GetByCorporationAsync(
        Guid corporationId,
        bool? isActive = null,
        CancellationToken ct = default);
}
