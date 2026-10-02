using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.DependencyInjection;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the NBP clients in a service collection.
/// </summary>
public static class NbpServiceCollectionExtensions
{
    /// <summary>The name of the <see cref="HttpClient"/> the NBP clients share.</summary>
    public const string HttpClientName = "OpenUrzednik.Nbp";

    /// <summary>
    /// Registers <see cref="INbpCurrencyExchangeRateClient"/>, <see cref="INbpExchangeRateTableClient"/> and <see cref="INbpGoldPriceClient"/>
    /// (and their classes) as typed clients of one named <see cref="HttpClient"/>, with validated <see cref="NbpOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The clients log through the registered <see cref="ILoggerFactory"/> (category: the client's full type name) and trace
    /// on the <c>ActivitySource</c> named <see cref="NbpTelemetry.SourceName"/>. A registered <see cref="TimeProvider"/> or
    /// <see cref="INbpUrlBuilderFactory"/> replaces the default. Invalid options throw an <see cref="OptionsValidationException"/>
    /// at startup (or when a client is first created, without a host).
    /// </para>
    /// <para>
    /// No resilience is added. Chain Microsoft's handler on the returned builder, e.g.
    /// <c>services.AddOpenUrzednikNbp().AddStandardResilienceHandler()</c>: its rejections (timeout, open circuit breaker, rate limiter)
    /// are returned as <c>RequestTimeoutError</c> and <c>ServiceUnavailableError</c>, not thrown. Add one resilience handler, not several.
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

        // Inserted first rather than appended, so it stays outside every resilience handler: one chained on the returned builder,
        // one added before this call, and one from ConfigureHttpClientDefaults, whose handlers come before the named client's.
        var builder = services.AddHttpClient(HttpClientName)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => handlers.Insert(0, new ResilienceRejectionHandler()));

        builder.AddTypedClient((httpClient, provider) => new NbpCurrencyExchangeRateClient(httpClient, GetOptions(provider),
            provider.GetService<INbpUrlBuilderFactory>(), provider.GetService<TimeProvider>(),
            CreateLogger<NbpCurrencyExchangeRateClient>(provider), TraceSource));
        builder.AddTypedClient((httpClient, provider) => new NbpExchangeRateTableClient(httpClient, GetOptions(provider),
            provider.GetService<INbpUrlBuilderFactory>(), provider.GetService<TimeProvider>(),
            CreateLogger<NbpExchangeRateTableClient>(provider), TraceSource));
        builder.AddTypedClient((httpClient, provider) => new NbpGoldPriceClient(httpClient, GetOptions(provider),
            provider.GetService<INbpUrlBuilderFactory>(), provider.GetService<TimeProvider>(),
            CreateLogger<NbpGoldPriceClient>(provider), TraceSource));

        services.TryAddTransient<INbpCurrencyExchangeRateClient>(provider => provider.GetRequiredService<NbpCurrencyExchangeRateClient>());
        services.TryAddTransient<INbpExchangeRateTableClient>(provider => provider.GetRequiredService<NbpExchangeRateTableClient>());
        services.TryAddTransient<INbpGoldPriceClient>(provider => provider.GetRequiredService<NbpGoldPriceClient>());

        return builder;
    }

    private static IOpenUrzednikTraceSource TraceSource => ActivityTraceSource.GetShared(NbpTelemetry.SourceName);

    private static NbpOptions GetOptions(IServiceProvider provider) => provider.GetRequiredService<IOptions<NbpOptions>>().Value;

    private static IOpenUrzednikLogger? CreateLogger<TClient>(IServiceProvider provider)
        => provider.GetService<ILoggerFactory>()?.CreateOpenUrzednikLogger<TClient>();

    // Marks that the clients are registered, so a second call doesn't add them, or the rejection handler, again.
    private sealed class NbpRegistration;
}
