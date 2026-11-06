using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenUrzednik.Http.DependencyInjection;

/// <summary>
/// Replaces <c>IHttpClientFactory</c>'s default loggers on a provider's named client with <see cref="DisplayPathHttpClientLogger"/> (ADR-0017).
/// For the DI package of a provider whose request paths or queries carry personal identifiers.
/// </summary>
internal static class DisplayPathHttpClientLogging
{
    /// <summary>
    /// Removes every logger of the client, including the default ones and those added through <c>ConfigureHttpClientDefaults</c>, and adds
    /// two <see cref="DisplayPathHttpClientLogger"/>s in the default loggers' places: <c>System.Net.Http.HttpClient.{name}.LogicalHandler</c>
    /// around the handler pipeline and <c>System.Net.Http.HttpClient.{name}.ClientHandler</c> next to the primary handler.
    /// An app restores Microsoft's loggers with <c>RemoveAllLoggers().AddDefaultLogger()</c> on the same builder.
    /// </summary>
    /// <param name="builder">The builder of the provider's named client.</param>
    /// <returns>The same builder.</returns>
    internal static IHttpClientBuilder AddOpenUrzednikDisplayPathLogging(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Microsoft's default loggers use "Default" for a client without a name.
        var category = "System.Net.Http.HttpClient." + (string.IsNullOrEmpty(builder.Name) ? "Default" : builder.Name);
        var pipelineCategory = category + ".LogicalHandler";
        var clientCategory = category + ".ClientHandler";

        return builder
            .RemoveAllLoggers()
            .AddLogger(provider => new DisplayPathHttpClientLogger(CreateLogger(provider, pipelineCategory), pipeline: true), wrapHandlersPipeline: true)
            .AddLogger(provider => new DisplayPathHttpClientLogger(CreateLogger(provider, clientCategory), pipeline: false), wrapHandlersPipeline: false);
    }

    private static ILogger CreateLogger(IServiceProvider provider, string category)
        => provider.GetService<ILoggerFactory>()?.CreateLogger(category) ?? NullLogger.Instance;
}
