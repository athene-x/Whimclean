using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Api.AspNetCore.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Api.AspNetCore.Tests;

public sealed class DependencyInjectionTests
{
    public sealed class ListWidgetsRequest : ListQueryRequest;

    public sealed class ListWidgetsRequestValidator : ListQueryRequestValidator<ListWidgetsRequest>;

    public sealed class ReplacementValidator : ListQueryRequestValidator<ListWidgetsRequest>;

    [Fact]
    public void AddMinimalApi_RegistersErrorHandlingAndOpenApi()
    {
        var services = new ServiceCollection();

        services.AddMinimalApi();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IApiExceptionMapper) &&
            descriptor.ImplementationType == typeof(DefaultApiExceptionMapper));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IExceptionHandler) &&
            descriptor.ImplementationType == typeof(ApiExceptionHandler));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType.FullName == "Microsoft.AspNetCore.OpenApi.OpenApiDocumentService" ||
            descriptor.ServiceType.Namespace == "Microsoft.AspNetCore.OpenApi");
    }

    [Fact]
    public void AddApiValidatorsFromAssembly_RegistersConcreteValidatorsOnce()
    {
        var services = new ServiceCollection();

        services.AddApiValidatorsFromAssemblyContaining<DependencyInjectionTests>();

        var descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(IValidator<ListWidgetsRequest>));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.DoesNotContain(services, candidate =>
            candidate.ImplementationType == typeof(ListQueryRequestValidator<ListWidgetsRequest>));
    }

    [Fact]
    public void AddApiValidatorsFromAssembly_KeepsExplicitRegistration()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ListWidgetsRequest>, ReplacementValidator>();

        services.AddApiValidatorsFromAssemblyContaining<DependencyInjectionTests>();

        var descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(IValidator<ListWidgetsRequest>));
        Assert.Equal(typeof(ReplacementValidator), descriptor.ImplementationType);
    }
}
