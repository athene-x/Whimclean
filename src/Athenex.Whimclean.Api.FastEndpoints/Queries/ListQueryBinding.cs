using Athenex.Whimclean.Api.AspNetCore.Queries;
using FastEndpoints;
using Microsoft.Extensions.Primitives;

namespace Athenex.Whimclean.Api.FastEndpoints.Queries;

/// <summary>
/// FastEndpoints binding adjustments for the shared
/// <see cref="ListQueryRequest" /> contract.
/// </summary>
public static class ListQueryBinding
{
    /// <summary>
    /// Binds <see cref="ListQueryRequest.Filters" /> from the repeated
    /// query parameter as-is, so a comma inside an <c>in</c> filter
    /// (<c>status:in:draft,published</c>) is not split into separate
    /// filters by the default array binding. Applied by
    /// <see cref="ApplicationBuilderExtensions.UseFastEndpointsApi" />;
    /// call it yourself only when configuring FastEndpoints directly.
    /// </summary>
    /// <returns>The same options, for chaining.</returns>
    public static BindingOptions KeepListQueryFiltersWhole(this BindingOptions binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        binding.ValueParserWhen(
            property =>
                property.Name == nameof(ListQueryRequest.Filters) &&
                typeof(ListQueryRequest).IsAssignableFrom(property.DeclaringType),
            (input, _) => input is StringValues filters
                ? new ParseResult(true, filters.ToArray())
                : new ParseResult(false, null));

        return binding;
    }
}
