using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Http.DependencyInjection;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.MF.WhiteList.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the MF White List clients in a service collection.
/// </summary>
public static class WhiteListServiceCollectionExtensions
{
    /// <summary>
    /// The name of the <see cref="HttpClient"/> of <see cref="AddOpenUrzednikMFWhiteList"/>, and the prefix of each key's client name
    /// (<see cref="AddKeyedOpenUrzednikMFWhiteList"/>).
    /// </summary>
    public const string HttpClientName = "OpenUrzednik.MF.WhiteList";

    /// <summary>
    /// Registers <see cref="IWhiteListClient"/> and <see cref="IWhiteListStatusClient"/>
    /// (and their classes) as typed clients of one named <see cref="HttpClient"/>, with validated <see cref="WhiteListOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The clients log through the registered <see cref="ILoggerFactory"/> (category: the client's full type name) and trace
    /// on the <c>ActivitySource</c> named <see cref="WhiteListTelemetry.SourceName"/>. A registered <see cref="TimeProvider"/>
    /// replaces the default. Invalid options throw an <see cref="OptionsValidationException"/>
    /// at startup (or when a client is first created, without a host).
    /// </para>
    /// <para>
    /// No resilience is added. Chain Microsoft's handler on the returned builder, e.g.
    /// <c>services.AddOpenUrzednikMFWhiteList().AddStandardResilienceHandler()</c>: its rejections (timeout, open circuit breaker, rate limiter)
    /// are returned as <c>RequestTimeoutError</c> and <c>ServiceUnavailableError</c> (without a status code), not thrown; a network failure is a
    /// <c>ConnectionFailedError</c>. Add one resilience handler, not several. A client you register yourself gets the same translation from
    /// <see cref="AddOpenUrzednikMFWhiteListRejectionHandler"/>. To retry on the API's own codes (the list is being updated), plug
    /// <see cref="WhiteListRetry.ShouldRetryAsync"/> into the handler's <c>Retry.ShouldHandle</c>.
    /// </para>
    /// <para>
    /// The requests carry NIP, REGON and account numbers in the path, so the named client's <c>IHttpClientFactory</c> log entries
    /// (<c>System.Net.Http.HttpClient.{name}.LogicalHandler</c> and <c>.ClientHandler</c>) show the display path instead, without the
    /// identifier (ADR-0017). <c>RemoveAllLoggers().AddDefaultLogger()</c> on the returned builder restores Microsoft's loggers, and with
    /// them the identifiers in the entries.
    /// </para>
    /// <para>
    /// Startup validation covers <see cref="WhiteListOptions"/> only: an invalid <see cref="HttpClient.BaseAddress"/> set on the returned builder
    /// throws an <see cref="ArgumentException"/> when a client is created.
    /// Calling this method again only applies <paramref name="configure"/>; the clients are registered once.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets the options, e.g. <see cref="WhiteListOptions.ApiUrl"/>; <see langword="null"/> keeps the defaults.</param>
    /// <returns>The builder of the shared <see cref="HttpClient"/>, for a resilience handler or other configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddOpenUrzednikMFWhiteList(this IServiceCollection services, Action<WhiteListOptions>? configure = null)
    {
        // Stryker disable once Statement : equivalent, AddOptions throws the same ArgumentNullException for services before anything is registered (checked by hand with Microsoft.Extensions 8 and 10)
        ArgumentNullException.ThrowIfNull(services);

        var options = services.AddOptions<WhiteListOptions>();
        if (configure is not null)
            options.Configure(configure);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(WhiteListRegistration)))
            return services.AddHttpClient(HttpClientName);

        services.AddSingleton<WhiteListRegistration>();
        services.TryAddSingleton(static provider => new WhiteListContainerMetrics(provider));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WhiteListOptions>, WhiteListOptionsValidation>());
        options.ValidateOnStart();

        var builder = services.AddHttpClient(HttpClientName).AddOpenUrzednikMFWhiteListRejectionHandler()
            .AddOpenUrzednikDisplayPathLogging();

        builder.AddTypedClient((httpClient, provider) => new WhiteListClient(httpClient, GetOptions(provider),
            provider.GetService<TimeProvider>(),
            Telemetry<WhiteListClient>(provider)));
        builder.AddTypedClient((httpClient, provider) => new WhiteListStatusClient(httpClient, GetOptions(provider),
            provider.GetService<TimeProvider>(),
            Telemetry<WhiteListStatusClient>(provider)));

        services.TryAddTransient<IWhiteListClient>(provider => provider.GetRequiredService<WhiteListClient>());
        services.TryAddTransient<IWhiteListStatusClient>(provider => provider.GetRequiredService<WhiteListStatusClient>());

        return builder;
    }

    /// <summary>
    /// Registers <see cref="IWhiteListClient"/> and <see cref="IWhiteListStatusClient"/>
    /// (and their classes) as keyed services under <paramref name="serviceKey"/>, with their own named <see cref="HttpClient"/>
    /// and validated <see cref="WhiteListOptions"/>, so an app can use several MF White List setups (e.g. a direct one and one through a gateway).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inject a client with <c>[FromKeyedServices("key")] IWhiteListClient client</c>, or resolve it with
    /// <c>GetRequiredKeyedService&lt;IWhiteListClient&gt;("key")</c>. The app's container must support keyed services, as
    /// Microsoft's dependency injection 8.0 and later does. The clients are transient, like those of <see cref="AddOpenUrzednikMFWhiteList"/>.
    /// </para>
    /// <para>
    /// Each key has its own <see cref="HttpClient"/>, named <see cref="HttpClientName"/> + <c>":"</c> + <paramref name="serviceKey"/>,
    /// and its own options, named <paramref name="serviceKey"/>; everything else (logging, tracing, a registered <see cref="TimeProvider"/>,
    /// startup validation, resilience rejections returned as errors) works as in <see cref="AddOpenUrzednikMFWhiteList"/>, which can be used
    /// alongside. Calling this method again with the same key only applies <paramref name="configure"/>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The key the clients are registered under; also the name of their options.</param>
    /// <param name="configure">Sets the options of this key, e.g. <see cref="WhiteListOptions.ApiUrl"/>; <see langword="null"/> keeps the defaults.</param>
    /// <returns>The builder of this key's <see cref="HttpClient"/>, for a resilience handler or other configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="serviceKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="serviceKey"/> is empty or white space.</exception>
    public static IHttpClientBuilder AddKeyedOpenUrzednikMFWhiteList(this IServiceCollection services, string serviceKey, Action<WhiteListOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceKey);

        var options = services.AddOptions<WhiteListOptions>(serviceKey);
        if (configure is not null)
            options.Configure(configure);

        var httpClientName = HttpClientName + ":" + serviceKey;
        if (services.Any(descriptor => descriptor.ServiceType == typeof(WhiteListKeyedRegistration) && Equals(descriptor.ServiceKey, serviceKey)))
            return services.AddHttpClient(httpClientName);

        // Resolved once per key, like the default clients' IOptions<WhiteListOptions>: a later configuration reload isn't picked up.
        services.AddKeyedSingleton(serviceKey, (provider, _) =>
            new WhiteListKeyedRegistration(provider.GetRequiredService<IOptionsMonitor<WhiteListOptions>>().Get(serviceKey)));
        services.TryAddSingleton(static provider => new WhiteListContainerMetrics(provider));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WhiteListOptions>, WhiteListOptionsValidation>());
        options.ValidateOnStart();

        var builder = services.AddHttpClient(httpClientName).AddOpenUrzednikMFWhiteListRejectionHandler()
            .AddOpenUrzednikDisplayPathLogging();

        services.TryAddKeyedTransient(serviceKey, (provider, _) => new WhiteListClient(CreateHttpClient(provider, httpClientName),
            GetOptions(provider, serviceKey), provider.GetService<TimeProvider>(),
            Telemetry<WhiteListClient>(provider)));
        services.TryAddKeyedTransient(serviceKey, (provider, _) => new WhiteListStatusClient(CreateHttpClient(provider, httpClientName),
            GetOptions(provider, serviceKey), provider.GetService<TimeProvider>(),
            Telemetry<WhiteListStatusClient>(provider)));

        services.TryAddKeyedTransient<IWhiteListClient>(serviceKey,
            (provider, key) => provider.GetRequiredKeyedService<WhiteListClient>(key));
        services.TryAddKeyedTransient<IWhiteListStatusClient>(serviceKey,
            (provider, key) => provider.GetRequiredKeyedService<WhiteListStatusClient>(key));

        return builder;
    }

    /// <summary>
    /// Makes an MF White List client registered without <see cref="AddOpenUrzednikMFWhiteList"/> return a resilience handler's rejections as errors:
    /// a timeout as a <c>RequestTimeoutError</c>, an open circuit breaker or a rate limiter as a <c>ServiceUnavailableError</c>
    /// (without a status code), instead of a thrown exception.
    /// </summary>
    /// <remarks>
    /// The handler goes first in the chain, outside every resilience handler, wherever that one is added (on this builder, before,
    /// or through <c>ConfigureHttpClientDefaults</c>). Calling this method again, or on the builder <see cref="AddOpenUrzednikMFWhiteList"/>
    /// returns, adds nothing: that builder already has it.
    /// </remarks>
    /// <param name="builder">The builder of the <see cref="HttpClient"/> the MF White List clients use.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddOpenUrzednikMFWhiteListRejectionHandler(this IHttpClientBuilder builder)
    {
        // Stryker disable once Statement : equivalent, ConfigureAdditionalHttpMessageHandlers throws the same ArgumentNullException for builder (checked by hand with Microsoft.Extensions 8 and 10)
        ArgumentNullException.ThrowIfNull(builder);

        // Inserted first rather than appended, so it stays outside every resilience handler: one chained on the builder later,
        // one added before this call, and one from ConfigureHttpClientDefaults, whose handlers come before the named client's.
        return builder.ConfigureAdditionalHttpMessageHandlers(static (handlers, _) =>
        {
            // Already in the list means an earlier call inserted it at 0; only an app inserting its own handler at 0 in between could
            // move it inward, which a handler meant to sit outside resilience doesn't do.
            foreach (var handler in handlers)
            {
                if (handler is ResilienceRejectionHandler)
                    return;
            }

            handlers.Insert(0, new ResilienceRejectionHandler());
        });
    }

    private static IOpenUrzednikTraceSource TraceSource => ActivityTraceSource.GetShared(WhiteListTelemetry.SourceName);

    private static WhiteListOptions GetOptions(IServiceProvider provider) => provider.GetRequiredService<IOptions<WhiteListOptions>>().Value;

    private static WhiteListOptions GetOptions(IServiceProvider provider, string serviceKey)
        => provider.GetRequiredKeyedService<WhiteListKeyedRegistration>(serviceKey).Options;

    private static HttpClient CreateHttpClient(IServiceProvider provider, string name) => provider.GetRequiredService<IHttpClientFactory>().CreateClient(name);

    private static IOpenUrzednikLogger? CreateLogger<TClient>(IServiceProvider provider)
        => provider.GetService<ILoggerFactory>()?.CreateOpenUrzednikLogger<TClient>();

    // One container per client: the logger's category is the client's type (ADR-0025).
    private static OpenUrzednikTelemetry Telemetry<TClient>(IServiceProvider provider)
        => new() { Logger = CreateLogger<TClient>(provider), TraceSource = TraceSource, Metrics = provider.GetRequiredService<WhiteListContainerMetrics>().Metrics };

    // The metrics of one container (ADR-0025): its IMeterFactory's OpenUrzednik.MF.WhiteList meter when it has a factory (on .NET 8 and
    // later AddHttpClient registers one), else the shared meter. A singleton, so the container's clients share one set of instruments.
    internal sealed class WhiteListContainerMetrics(IServiceProvider provider)
    {
        public IOpenUrzednikMetrics Metrics { get; } = For(provider.GetService<IMeterFactory>());

        // A method of its own, so a test can pass no factory on .NET too, where AddHttpClient always registers one (and needs it).
        internal static IOpenUrzednikMetrics For(IMeterFactory? meterFactory)
            => meterFactory is not null ? MeterMetrics.Create(meterFactory, WhiteListTelemetry.SourceName) : MeterMetrics.GetShared(WhiteListTelemetry.SourceName);
    }

    // Marks that the clients are registered, so a second call doesn't add them, or the rejection handler, again.
    private sealed class WhiteListRegistration;

    // Marks a key whose clients are registered, so a second call with that key doesn't add them again, and holds its options.
    private sealed class WhiteListKeyedRegistration(WhiteListOptions options)
    {
        public WhiteListOptions Options { get; } = options;
    }
}
