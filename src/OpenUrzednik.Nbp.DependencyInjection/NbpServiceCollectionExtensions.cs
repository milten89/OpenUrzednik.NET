using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the NBP clients in a service collection.
/// </summary>
public static class NbpServiceCollectionExtensions
{
    /// <summary>
    /// The name of the <see cref="HttpClient"/> of <see cref="AddOpenUrzednikNbp"/>, and the prefix of each key's client name
    /// (<see cref="AddKeyedOpenUrzednikNbp"/>).
    /// </summary>
    public const string HttpClientName = "OpenUrzednik.Nbp";

    /// <summary>
    /// Registers <see cref="INbpCurrencyExchangeRateClient"/>, <see cref="INbpExchangeRateTableClient"/> and <see cref="INbpGoldPriceClient"/>
    /// (and their classes) as typed clients of one named <see cref="HttpClient"/>, with validated <see cref="NbpOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The clients log through the registered <see cref="ILoggerFactory"/> (category: the client's full type name) and trace
    /// on the <c>ActivitySource</c> named <see cref="NbpTelemetry.SourceName"/>. A registered <see cref="TimeProvider"/>
    /// replaces the default. Invalid options throw an <see cref="OptionsValidationException"/>
    /// at startup (or when a client is first created, without a host).
    /// </para>
    /// <para>
    /// No resilience is added. Chain Microsoft's handler on the returned builder, e.g.
    /// <c>services.AddOpenUrzednikNbp().AddStandardResilienceHandler()</c>: its rejections (timeout, open circuit breaker, rate limiter)
    /// are returned as <c>RequestTimeoutError</c> and <c>ServiceUnavailableError</c> (without a status code), not thrown; a network failure is a
    /// <c>ConnectionFailedError</c>. Add one resilience handler, not several. A client you register yourself gets the same translation from
    /// <see cref="AddOpenUrzednikNbpRejectionHandler"/>.
    /// </para>
    /// <para>
    /// Startup validation covers <see cref="NbpOptions"/> only: an invalid <see cref="HttpClient.BaseAddress"/> set on the returned builder
    /// throws an <see cref="ArgumentException"/> when a client is created.
    /// Calling this method again only applies <paramref name="configure"/>; the clients are registered once.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets the options, e.g. <see cref="NbpOptions.ApiUrl"/>; <see langword="null"/> keeps the defaults.</param>
    /// <returns>The builder of the shared <see cref="HttpClient"/>, for a resilience handler or other configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddOpenUrzednikNbp(this IServiceCollection services, Action<NbpOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.AddOptions<NbpOptions>();
        if (configure is not null)
            options.Configure(configure);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(NbpRegistration)))
            return services.AddHttpClient(HttpClientName);

        services.AddSingleton<NbpRegistration>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<NbpOptions>, NbpOptionsValidation>());
        options.ValidateOnStart();

        var builder = services.AddHttpClient(HttpClientName).AddOpenUrzednikNbpRejectionHandler();

        builder.AddTypedClient((httpClient, provider) => new NbpCurrencyExchangeRateClient(httpClient, GetOptions(provider),
            provider.GetService<TimeProvider>(),
            CreateLogger<NbpCurrencyExchangeRateClient>(provider), TraceSource));
        builder.AddTypedClient((httpClient, provider) => new NbpExchangeRateTableClient(httpClient, GetOptions(provider),
            provider.GetService<TimeProvider>(),
            CreateLogger<NbpExchangeRateTableClient>(provider), TraceSource));
        builder.AddTypedClient((httpClient, provider) => new NbpGoldPriceClient(httpClient, GetOptions(provider),
            provider.GetService<TimeProvider>(),
            CreateLogger<NbpGoldPriceClient>(provider), TraceSource));

        services.TryAddTransient<INbpCurrencyExchangeRateClient>(provider => provider.GetRequiredService<NbpCurrencyExchangeRateClient>());
        services.TryAddTransient<INbpExchangeRateTableClient>(provider => provider.GetRequiredService<NbpExchangeRateTableClient>());
        services.TryAddTransient<INbpGoldPriceClient>(provider => provider.GetRequiredService<NbpGoldPriceClient>());

        return builder;
    }

    /// <summary>
    /// Registers <see cref="INbpCurrencyExchangeRateClient"/>, <see cref="INbpExchangeRateTableClient"/> and <see cref="INbpGoldPriceClient"/>
    /// (and their classes) as keyed services under <paramref name="serviceKey"/>, with their own named <see cref="HttpClient"/>
    /// and validated <see cref="NbpOptions"/>, so an app can use several NBP setups (e.g. a direct one and one through a gateway).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inject a client with <c>[FromKeyedServices("key")] INbpGoldPriceClient client</c>, or resolve it with
    /// <c>GetRequiredKeyedService&lt;INbpGoldPriceClient&gt;("key")</c>. The app's container must support keyed services, as
    /// Microsoft's dependency injection 8.0 and later does. The clients are transient, like those of <see cref="AddOpenUrzednikNbp"/>.
    /// </para>
    /// <para>
    /// Each key has its own <see cref="HttpClient"/>, named <see cref="HttpClientName"/> + <c>":"</c> + <paramref name="serviceKey"/>,
    /// and its own options, named <paramref name="serviceKey"/>; everything else (logging, tracing, a registered <see cref="TimeProvider"/>,
    /// startup validation, resilience rejections returned as errors) works as in <see cref="AddOpenUrzednikNbp"/>, which can be used
    /// alongside. Calling this method again with the same key only applies <paramref name="configure"/>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The key the clients are registered under; also the name of their options.</param>
    /// <param name="configure">Sets the options of this key, e.g. <see cref="NbpOptions.ApiUrl"/>; <see langword="null"/> keeps the defaults.</param>
    /// <returns>The builder of this key's <see cref="HttpClient"/>, for a resilience handler or other configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="serviceKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="serviceKey"/> is empty or white space.</exception>
    public static IHttpClientBuilder AddKeyedOpenUrzednikNbp(this IServiceCollection services, string serviceKey, Action<NbpOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceKey);

        var options = services.AddOptions<NbpOptions>(serviceKey);
        if (configure is not null)
            options.Configure(configure);

        var httpClientName = HttpClientName + ":" + serviceKey;
        if (services.Any(descriptor => descriptor.ServiceType == typeof(NbpKeyedRegistration) && Equals(descriptor.ServiceKey, serviceKey)))
            return services.AddHttpClient(httpClientName);

        // Resolved once per key, like the default clients' IOptions<NbpOptions>: a later configuration reload isn't picked up.
        services.AddKeyedSingleton(serviceKey, (provider, _) =>
            new NbpKeyedRegistration(provider.GetRequiredService<IOptionsMonitor<NbpOptions>>().Get(serviceKey)));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<NbpOptions>, NbpOptionsValidation>());
        options.ValidateOnStart();

        var builder = services.AddHttpClient(httpClientName).AddOpenUrzednikNbpRejectionHandler();

        services.TryAddKeyedTransient(serviceKey, (provider, _) => new NbpCurrencyExchangeRateClient(CreateHttpClient(provider, httpClientName),
            GetOptions(provider, serviceKey), provider.GetService<TimeProvider>(),
            CreateLogger<NbpCurrencyExchangeRateClient>(provider), TraceSource));
        services.TryAddKeyedTransient(serviceKey, (provider, _) => new NbpExchangeRateTableClient(CreateHttpClient(provider, httpClientName),
            GetOptions(provider, serviceKey), provider.GetService<TimeProvider>(),
            CreateLogger<NbpExchangeRateTableClient>(provider), TraceSource));
        services.TryAddKeyedTransient(serviceKey, (provider, _) => new NbpGoldPriceClient(CreateHttpClient(provider, httpClientName),
            GetOptions(provider, serviceKey), provider.GetService<TimeProvider>(),
            CreateLogger<NbpGoldPriceClient>(provider), TraceSource));

        services.TryAddKeyedTransient<INbpCurrencyExchangeRateClient>(serviceKey,
            (provider, key) => provider.GetRequiredKeyedService<NbpCurrencyExchangeRateClient>(key));
        services.TryAddKeyedTransient<INbpExchangeRateTableClient>(serviceKey,
            (provider, key) => provider.GetRequiredKeyedService<NbpExchangeRateTableClient>(key));
        services.TryAddKeyedTransient<INbpGoldPriceClient>(serviceKey,
            (provider, key) => provider.GetRequiredKeyedService<NbpGoldPriceClient>(key));

        return builder;
    }

    /// <summary>
    /// Makes an NBP client registered without <see cref="AddOpenUrzednikNbp"/> return a resilience handler's rejections as errors:
    /// a timeout as a <c>RequestTimeoutError</c>, an open circuit breaker or a rate limiter as a <c>ServiceUnavailableError</c>
    /// (without a status code), instead of a thrown exception.
    /// </summary>
    /// <remarks>
    /// The handler goes first in the chain, outside every resilience handler, wherever that one is added (on this builder, before,
    /// or through <c>ConfigureHttpClientDefaults</c>). Calling this method again, or on the builder <see cref="AddOpenUrzednikNbp"/>
    /// returns, adds nothing: that builder already has it.
    /// </remarks>
    /// <param name="builder">The builder of the <see cref="HttpClient"/> the NBP clients use.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddOpenUrzednikNbpRejectionHandler(this IHttpClientBuilder builder)
    {
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

    private static IOpenUrzednikTraceSource TraceSource => ActivityTraceSource.GetShared(NbpTelemetry.SourceName);

    private static NbpOptions GetOptions(IServiceProvider provider) => provider.GetRequiredService<IOptions<NbpOptions>>().Value;

    private static NbpOptions GetOptions(IServiceProvider provider, string serviceKey)
        => provider.GetRequiredKeyedService<NbpKeyedRegistration>(serviceKey).Options;

    private static HttpClient CreateHttpClient(IServiceProvider provider, string name) => provider.GetRequiredService<IHttpClientFactory>().CreateClient(name);

    private static IOpenUrzednikLogger? CreateLogger<TClient>(IServiceProvider provider)
        => provider.GetService<ILoggerFactory>()?.CreateOpenUrzednikLogger<TClient>();

    // Marks that the clients are registered, so a second call doesn't add them, or the rejection handler, again.
    private sealed class NbpRegistration;

    // Marks a key whose clients are registered, so a second call with that key doesn't add them again, and holds its options.
    private sealed class NbpKeyedRegistration(NbpOptions options)
    {
        public NbpOptions Options { get; } = options;
    }
}
