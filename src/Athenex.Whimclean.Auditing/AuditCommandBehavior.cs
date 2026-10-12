using System.Linq.Expressions;
using Athenex.Whimclean.Domain.Results;
using Mediator;

namespace Athenex.Whimclean.Auditing;

/// <summary>
/// Wraps every <see cref="IAuditedCommand" /> and records its outcome:
/// success, a failed <see cref="Result" /> with its error code, or an
/// exception with its type name (which is rethrown). Register it before
/// the unit-of-work behavior so it runs outside the transaction and sees
/// the committed outcome; <c>AddAuditing</c> does this when called before
/// <c>AddUnitOfWorkBehavior</c>.
/// </summary>
/// <typeparam name="TMessage">Command being handled.</typeparam>
/// <typeparam name="TResponse">Handler response type.</typeparam>
public sealed class AuditCommandBehavior<TMessage, TResponse>(IAuditLogRecorder recorder)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IAuditedCommand, ICommand<TResponse>
{
    private static readonly Func<TResponse, string?> CreatedId = BuildCreatedIdAccessor();

    /// <summary>
    /// Runs the rest of the pipeline, then writes the entry. The write
    /// is not cancellable: an outcome that happened must be recorded.
    /// </summary>
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var descriptor = message.Describe();
        TResponse response;

        try
        {
            response = await next(message, cancellationToken);
        }
        catch (Exception exception)
        {
            await recorder.RecordAsync(
                descriptor,
                AuditResult.Failed,
                exception.GetType().Name,
                createdId: null,
                CancellationToken.None);
            throw;
        }

        if (response is Result { IsFailure: true } failure)
        {
            await recorder.RecordAsync(
                descriptor,
                AuditResult.Failed,
                failure.Error.Code,
                createdId: null,
                CancellationToken.None);
        }
        else
        {
            await recorder.RecordAsync(
                descriptor,
                AuditResult.Success,
                errorCode: null,
                CreatedId(response),
                CancellationToken.None);
        }

        return response;
    }

    private static Func<TResponse, string?> BuildCreatedIdAccessor()
    {
        var type = typeof(TResponse);

        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Result<>))
        {
            return _ => null;
        }

        var valueType = type.GetGenericArguments()[0];

        if (!valueType.IsPrimitive && valueType != typeof(Guid) && valueType != typeof(string))
        {
            return _ => null;
        }

        // Compile the Value read once per closed type instead of reflecting
        // on every command.
        var response = Expression.Parameter(type, "response");
        var readValue = Expression.Lambda<Func<TResponse, object?>>(
            Expression.Convert(
                Expression.Property(response, nameof(Result<object>.Value)),
                typeof(object)),
            response).Compile();

        return response => response is Result { IsSuccess: true }
            ? readValue(response)?.ToString()
            : null;
    }
}
