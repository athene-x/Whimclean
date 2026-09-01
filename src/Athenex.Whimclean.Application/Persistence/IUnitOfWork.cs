namespace Athenex.Whimclean.Application.Persistence;

/// <summary>
/// Commit boundary of a business operation: everything changed within the
/// current scope is persisted as one atomic unit.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes atomically and then dispatches any
    /// domain events queued on the changed aggregates. A no-op when
    /// nothing changed.
    /// </summary>
    /// <exception cref="PersistenceConcurrencyException">
    /// A record changed after it was loaded; reload it, reapply the
    /// change, and retry.
    /// </exception>
    Task CommitAsync(
        CancellationToken cancellationToken = default);
}
