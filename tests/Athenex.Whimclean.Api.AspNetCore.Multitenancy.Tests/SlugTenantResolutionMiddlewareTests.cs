using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy.Tests;

public sealed class SlugTenantResolutionMiddlewareTests
{
    private sealed class DictionaryLookup(Dictionary<string, Guid> tenants) : ITenantSlugLookup<Guid>
    {
        public List<string> Slugs { get; } = [];

        public ValueTask<Guid> FindBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            Slugs.Add(slug);
            return ValueTask.FromResult(tenants.GetValueOrDefault(slug));
        }
    }

    private static readonly IOptions<SlugTenantResolutionOptions> DefaultOptions =
        Options.Create(new SlugTenantResolutionOptions());

    private static DefaultHttpContext ContextWithSlug(params string[] values)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Slug"] = values;
        return context;
    }

    [Fact]
    public async Task Middleware_RecordsTheTenantNamedByTheNormalizedSlug()
    {
        var tenantId = Guid.NewGuid();
        var lookup = new DictionaryLookup(new() { ["acme"] = tenantId });
        var tenantContext = new TenantContext<Guid>();
        var reachedNext = false;
        var middleware = new SlugTenantResolutionMiddleware<Guid>(_ =>
        {
            reachedNext = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(ContextWithSlug("  ACME "), DefaultOptions, lookup, tenantContext);

        Assert.True(reachedNext);
        Assert.Equal(["acme"], lookup.Slugs);
        Assert.True(tenantContext.TryGetRequestedTenantId(out var requested));
        Assert.Equal(tenantId, requested);
        Assert.False(tenantContext.TryGetTenantId(out _));
    }

    [Fact]
    public async Task Middleware_RecordsNothingForUnknownMissingOrRepeatedSlugs()
    {
        var lookup = new DictionaryLookup([]);
        var middleware = new SlugTenantResolutionMiddleware<Guid>(_ => Task.CompletedTask);

        foreach (var context in new[]
                 {
                     ContextWithSlug("unknown"),
                     new DefaultHttpContext(),
                     ContextWithSlug("a", "b")
                 })
        {
            var tenantContext = new TenantContext<Guid>();

            await middleware.InvokeAsync(context, DefaultOptions, lookup, tenantContext);

            Assert.False(tenantContext.TryGetRequestedTenantId(out _));
        }

        Assert.Equal(["unknown"], lookup.Slugs);
    }

    [Fact]
    public async Task Middleware_KeepsAnAlreadyRecordedTenant()
    {
        var headerTenantId = Guid.NewGuid();
        var lookup = new DictionaryLookup(new() { ["acme"] = Guid.NewGuid() });
        var tenantContext = new TenantContext<Guid>();
        var context = ContextWithSlug("acme");
        context.Request.Headers["X-Tenant-Id"] = headerTenantId.ToString();
        var middleware = new SlugTenantResolutionMiddleware<Guid>(_ => Task.CompletedTask);

        await new TenantResolutionMiddleware<Guid>(_ => Task.CompletedTask).InvokeAsync(
            context,
            new HeaderRequestedTenantResolver<Guid>(Options.Create(new HeaderTenantResolutionOptions())),
            tenantContext);
        await middleware.InvokeAsync(context, DefaultOptions, lookup, tenantContext);

        Assert.Empty(lookup.Slugs);
        Assert.True(tenantContext.TryGetRequestedTenantId(out var requested));
        Assert.Equal(headerTenantId, requested);
    }

    [Fact]
    public void AddSlugTenantResolution_ConfiguresTheHeaderName()
    {
        var services = new ServiceCollection();
        services.AddSlugTenantResolution(options => options.HeaderName = "X-Org");
        using var provider = services.BuildServiceProvider();

        Assert.Equal("X-Org", provider.GetRequiredService<IOptions<SlugTenantResolutionOptions>>().Value.HeaderName);
    }
}
