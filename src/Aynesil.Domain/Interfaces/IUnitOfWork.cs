namespace Aynesil.Domain.Interfaces;

/// <summary>
/// Wraps the EF Core DbContext transaction boundary.
/// Commands call SaveChangesAsync to commit; the interceptor dispatches
/// domain events and updates audit fields within the same call.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
