using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;

using Shouldly;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

public sealed partial class NbpServiceCollectionExtensionsTest
{
    [Fact]
    public void AddOpenUrzednikNbp_NoConfiguration_ResolvesEveryClient()
    {
        // Arrange
        using var provider = BuildProvider(out _);

        // Act & Assert
        provider.GetRequiredService<INbpCurrencyExchangeRateClient>().ShouldBeOfType<NbpCurrencyExchangeRateClient>();
        provider.GetRequiredService<INbpExchangeRateTableClient>().ShouldBeOfType<NbpExchangeRateTableClient>();
        provider.GetRequiredService<INbpGoldPriceClient>().ShouldBeOfType<NbpGoldPriceClient>();
        provider.GetRequiredService<NbpGoldPriceClient>().ShouldNotBeSameAs(provider.GetRequiredService<NbpGoldPriceClient>());
    }

    [Fact]
    public void AddOpenUrzednikNbp_Services_ReturnsBuilderOfTheSharedHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddOpenUrzednikNbp();

        // Assert
        builder.Name.ShouldBe(NbpServiceCollectionExtensions.HttpClientName);
    }

    [Fact]
    public async Task AddOpenUrzednikNbp_NoApiUrl_CallsPublicApi()
    {
        // Arrange
        using var provider = BuildProvider(out var handler);

        // Act
        var result = await provider.GetRequiredService<INbpGoldPriceClient>().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://api.nbp.pl/api/cenyzlota"));
    }

    [Fact]
    public async Task AddOpenUrzednikNbp_ApiUrlConfigured_ClientCallsIt()
    {
        // Arrange
        using var provider = BuildProvider(out var handler,
            services => services.AddOpenUrzednikNbp(options => options.ApiUrl = "https://proxy.example.com/nbp"));

        // Act
        await provider.GetRequiredService<INbpGoldPriceClient>().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://proxy.example.com/nbp/cenyzlota"));
    }

    [Fact]
    public void AddOpenUrzednikNbp_InvalidOptions_FailsStartupValidation()
    {
        // Arrange
        using var provider = BuildProvider(out _, services => services.AddOpenUrzednikNbp(options => options.ApiUrl = "http://api.nbp.pl/api/"));

        // Act
        var exception = Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());

        // Assert
        exception.Message.ShouldContain("NBP API no longer supports HTTP");
    }

    [Fact]
    public void AddOpenUrzednikNbp_InvalidOptionsWithoutHost_ThrowsWhenAClientIsCreated()
    {
        // Arrange
        using var provider = BuildProvider(out _, services => services.AddOpenUrzednikNbp(options => options.Timeout = TimeSpan.Zero));

        // Act & Assert
        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<INbpGoldPriceClient>());
    }

    [Fact]
    public void AddOpenUrzednikNbp_CalledTwice_RegistersOnceAndAppliesBothConfigurations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOpenUrzednikNbp(options => options.Timeout = TimeSpan.FromSeconds(5));
        services.AddOpenUrzednikNbp(options => options.ApiUrl = "https://proxy.example.com/nbp/");

        // Assert
        services.Count(descriptor => descriptor.ServiceType == typeof(NbpGoldPriceClient)).ShouldBe(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(INbpGoldPriceClient)).ShouldBe(1);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NbpOptions>>().Value;
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        options.ApiUrl.ShouldBe("https://proxy.example.com/nbp/");
        var factoryOptions = provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>().Get(NbpServiceCollectionExtensions.HttpClientName);
        factoryOptions.HttpMessageHandlerBuilderActions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddOpenUrzednikNbp_UrlBuilderFactoryRegistered_ClientUsesIt()
    {
        // Arrange
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns("custom/latest");
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        urlBuilderFactory.GetGoldBuilder().Returns(urlBuilder);
        using var provider = BuildProvider(out var handler, services => services.AddSingleton(urlBuilderFactory));

        // Act
        await provider.GetRequiredService<INbpGoldPriceClient>().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://api.nbp.pl/api/custom/latest"));
    }

    [Fact]
    public async Task AddOpenUrzednikNbp_LoggingRegistered_ClientLogsUnderItsTypeName()
    {
        // Arrange
        var loggerProvider = new RecordingLoggerProvider();
        using var provider = BuildProvider(out _, services => services.AddLogging(logging => logging
            .AddProvider(loggerProvider)
            .SetMinimumLevel(LogLevel.Debug)));

        // Act
        await provider.GetRequiredService<INbpGoldPriceClient>().GetTopCountAsync(0, TestContext.Current.CancellationToken);

        // Assert
        loggerProvider.Entries.ShouldContain(entry =>
            entry.Category == "OpenUrzednik.Nbp.Gold.NbpGoldPriceClient" &&
            entry.Level == LogLevel.Debug &&
            entry.Message == "Validation failed for GetTopCountAsync");
    }

    [Fact]
    public async Task AddOpenUrzednikNbp_ListenerOnNbpSource_RecordsClientSpans()
    {
        // Arrange
        // Other tests may also trace NBP calls, so only spans under this test's own parent count.
        using var parentSource = new ActivitySource($"OpenUrzednik.Tests.{Guid.NewGuid():N}");
        var stopped = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source == parentSource || source.Name == NbpTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (stopped)
                    stopped.Add(activity);
            },
        };
        ActivitySource.AddActivityListener(listener);
        using var provider = BuildProvider(out _);
        var client = provider.GetRequiredService<INbpGoldPriceClient>();

        // Act
        ActivityTraceId traceId;
        using (var parent = parentSource.StartActivity("test"))
        {
            traceId = parent!.TraceId;
            await client.GetLatestAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        List<string> names;
        lock (stopped)
            names = [.. stopped.Where(a => a.TraceId == traceId && a.Source.Name == NbpTelemetry.SourceName).Select(a => a.OperationName)];
        names.ShouldBe(["nbp.http.get", "nbp.gold.latest"], ignoreOrder: true);
    }

    [Fact]
    public void AddOpenUrzednikNbp_NullServices_ThrowsArgumentNullException()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddOpenUrzednikNbp());

        // Assert
        exception.ParamName.ShouldBe("services");
    }
}
