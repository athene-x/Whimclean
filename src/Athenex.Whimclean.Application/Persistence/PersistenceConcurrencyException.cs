namespace Athenex.Whimclean.Application.Persistence;

/// <summary>
/// Provider-neutral optimistic-concurrency failure: a record changed after
/// it was loaded. Callers should reload the record, reapply the change,
/// and retry the commit.
/// </summary>
/// <param name="message">Description of the conflicting write.</param>
/// <param name="innerException">
/// The provider-specific concurrency exception being wrapped.
/// </param>
public sealed class PersistenceConcurrencyException(
    string message,
    Exception innerException) : Exception(message, innerException);
