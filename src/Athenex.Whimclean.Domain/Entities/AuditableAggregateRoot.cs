using Athenex.Whimclean.Domain.Auditables;

namespace Athenex.Whimclean.Domain.Entities;

/// <summary>
/// An <see cref="AggregateRoot{TId}" /> with the creation and
/// modification audit fields the EF infrastructure stamps at save time,
/// so each aggregate does not redeclare them.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
/// <typeparam name="TActor">
/// The stored actor type; nullable when system writes leave it unset.
/// </typeparam>
public abstract class AuditableAggregateRoot<TId, TActor> : AggregateRoot<TId>, IAuditable<TActor>
    where TId : notnull
{
    /// <inheritdoc cref="AggregateRoot{TId}(TId)" />
    protected AuditableAggregateRoot(TId id) : base(id)
    {
    }

    /// <inheritdoc cref="AggregateRoot{TId}()" />
    protected AuditableAggregateRoot()
    {
    }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public TActor CreatedBy { get; private set; } = default!;

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <inheritdoc />
    public TActor? UpdatedBy { get; private set; }
}
