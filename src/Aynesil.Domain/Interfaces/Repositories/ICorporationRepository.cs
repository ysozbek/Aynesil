using Aynesil.Domain.Modules.Core.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Typed repository for <see cref="Corporation"/> aggregate root.
/// Extends the generic repository with corporation-specific query operations.
/// </summary>
public interface ICorporationRepository : IRepository<Corporation>
{
    /// <summary>Returns the corporation identified by its machine slug code.</summary>
    Task<Corporation?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the given code is already taken.
    /// Pass <paramref name="excludeId"/> to allow the current corporation to keep its own code during updates.
    /// </summary>
    Task<bool> IsCodeTakenAsync(string code, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>Returns the active campus count for a corporation without loading the collection.</summary>
    Task<int> GetCampusCountAsync(Guid corporationId, CancellationToken ct = default);
}
