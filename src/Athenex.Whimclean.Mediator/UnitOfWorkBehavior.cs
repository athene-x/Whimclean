using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Domain.Results;
using Mediator;

namespace Athenex.Whimclean.Mediator;

/// <summary>
/// Commits the unit of work after a command handler completes, so
/// handlers mutate aggregates without calling
/// <see cref="IUnitOfWork.CommitAsync" /> themselves and one command spans
/// exactly one commit. The commit is skipped when the handler returns a
/// failed <see cref="Result" /> or throws, leaving pending changes
/// unpersisted. Queries and notifications are unaffected: the behavior
/// only binds to messages implementing <see cref="ICommand{TResponse}" />.
/// </summary>
/// <typeparam name="TMessage">Command being handled.</typeparam>
/// <typeparam name="TResponse">Handler response type.</typeparam>
public sealed class UnitOfWorkBehavior<TMessage, TResponse>(
    IUnitOfWork unitOfWork) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, ICommand<TResponse>
{
    /// <summary>
    /// Invokes the rest of the pipeline, then commits unless the response
    /// is a failed <see cref="Result" />.
    /// </summary>
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(message, cancellationToken);

        if (response is not Result { IsFailure: true })
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return response;
    }
}
