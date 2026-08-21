using Athenex.Whimclean.Api.FastEndpoints.Errors;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Api.FastEndpoints;

public static class DependencyInjection
{
    public static IServiceCollection AddFastEndpointsApi(
        this IServiceCollection services,
        Action<DocumentOptions>? configureOpenApi = null)
    {
        services.AddFastEndpoints();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IApiExceptionMapper,
                DefaultApiExceptionMapper>());
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();

        if (configureOpenApi is null)
        {
            services.OpenApiDocument();
        }
        else
        {
            services.OpenApiDocument(configureOpenApi);
        }

        return services;
    }
}
