using Athenex.Whimclean.Api.AspNetCore.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Api.AspNetCore.Validation;

/// <summary>
/// Endpoint filter factory that validates handler arguments with the
/// <see cref="IValidator{T}"/> registered for their type and short-circuits
/// with the shared 400 response. Attached through
/// <see cref="EndpointRouteBuilderExtensions.WithRequestValidation{TBuilder}"/>.
/// </summary>
internal static class RequestValidationFilter
{
    /// <summary>
    /// Inspects the handler once at startup and returns
    /// <paramref name="next"/> unchanged when no argument type has a
    /// validator registered, so unvalidated endpoints pay nothing.
    /// </summary>
    public static EndpointFilterDelegate Create(
        EndpointFilterFactoryContext context,
        EndpointFilterDelegate next)
    {
        var parameters = context.MethodInfo.GetParameters();
        var isService = context.ApplicationServices.GetService<IServiceProviderIsService>();
        List<(int Index, Type ValidatorType)>? targets = null;

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameterType = parameters[index].ParameterType;
            if (!IsCandidate(parameterType))
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(parameterType);
            if (isService is not null && !isService.IsService(validatorType))
            {
                continue;
            }

            (targets ??= []).Add((index, validatorType));
        }

        if (targets is null)
        {
            return next;
        }

        var validated = targets.ToArray();

        return async invocationContext =>
        {
            var httpContext = invocationContext.HttpContext;

            foreach (var (index, validatorType) in validated)
            {
                if (invocationContext.Arguments[index] is not { } argument ||
                    httpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                {
                    continue;
                }

                var result = await validator.ValidateAsync(
                    new ValidationContext<object>(argument),
                    httpContext.RequestAborted);

                if (!result.IsValid)
                {
                    return result.ToHttpResult();
                }
            }

            return await next(invocationContext);
        };
    }

    private static bool IsCandidate(Type type) =>
        type is { IsValueType: false, IsArray: false, IsPointer: false, IsByRef: false } &&
        type != typeof(string) &&
        type.Namespace is { } ns &&
        !ns.StartsWith("System", StringComparison.Ordinal) &&
        !ns.StartsWith("Microsoft", StringComparison.Ordinal);
}
