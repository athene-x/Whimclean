namespace Athenex.Whimclean.Application.Context;

/// <summary>
/// Thrown when a write is attempted with no authenticated current user:
/// every change persisted through an auditable context must be attributable
/// to someone. Hosts without an authenticated principal (workers, seeders,
/// migrations) avoid this by registering
/// <see cref="StaticCurrentUser{TUserId}" /> with a designated system
/// account.
/// </summary>
public sealed class CurrentUserRequiredException()
    : Exception("A current user is required to persist changes.");
