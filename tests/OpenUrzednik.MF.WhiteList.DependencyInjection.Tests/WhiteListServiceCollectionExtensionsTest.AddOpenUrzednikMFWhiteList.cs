using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

public sealed partial class WhiteListServiceCollectionExtensionsTest
{
    [Fact]
    public void AddOpenUrzednikMFWhiteList_NoConfiguration_ResolvesEveryClient()
    {
        // Arrange
        using var provider = BuildProvider(out _);

        // Act & Assert
        provider.GetRequiredService<IWhiteListStatusClient>().ShouldBeOfType<WhiteListStatusClient>();
        provider.GetRequiredService<IWhiteListClient>().ShouldBeOfType<WhiteListClient>();
        provider.GetRequiredService<WhiteListClient>().ShouldNotBeSameAs(provider.GetRequiredService<WhiteListClient>());
    }

    [Fact]
    public void AddOpenUrzednikMFWhiteList_Services_ReturnsBuilderOfTheSharedHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddOpenUrzednikMFWhiteList();

        // Assert
        builder.Name.ShouldBe(WhiteListServiceCollectionExtensions.HttpClientName);
    }

    [Fact]
    public async Task AddOpenUrzednikMFWhiteList_NoApiUrl_CallsPublicApi()
    {
        // Arrange
        using var provider = BuildProvider(out var handler);

        // Act
        var result = await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/3245174504?date=2026-11-02"));
    }

    [Fact]
    public async Task AddOpenUrzednikMFWhiteList_ApiUrlConfigured_ClientCallsIt()
    {
        // Arrange
        using var provider = BuildProvider(out var handler,
            services => services.AddOpenUrzednikMFWhiteList(options => options.ApiUrl = "https://proxy.example.com/"));

        // Act
        await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://proxy.example.com/api/search/nip/3245174504?date=2026-11-02"));
    }

    [Fact]
    public void AddOpenUrzednikMFWhiteList_InvalidOptions_FailsStartupValidation()
    {
        // Arrange
        using var provider = BuildProvider(out _, services => services.AddOpenUrzednikMFWhiteList(options => options.ApiUrl = "http://wykazpodatnikowvat.mf.gov.pl/"));

        // Act
        var exception = Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());

        // Assert
        exception.Message.ShouldContain("supports only HTTPS");
    }

    [Fact]
    public void AddOpenUrzednikMFWhiteList_InvalidOptionsWithoutHost_ThrowsWhenAClientIsCreated()
    {
        // Arrange
        using var provider = BuildProvider(out _, services => services.AddOpenUrzednikMFWhiteList(options => options.Timeout = TimeSpan.Zero));

        // Act & Assert
        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IWhiteListClient>());
    }

    [Fact]
    public void AddOpenUrzednikMFWhiteList_CalledTwice_RegistersOnceAndAppliesBothConfigurations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOpenUrzednikMFWhiteList(options => options.Timeout = TimeSpan.FromSeconds(5));
        services.AddOpenUrzednikMFWhiteList(options => options.ApiUrl = "https://proxy.example.com/");

        // Assert
        services.Count(descriptor => descriptor.ServiceType == typeof(WhiteListClient)).ShouldBe(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(IWhiteListClient)).ShouldBe(1);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<WhiteListOptions>>().Value;
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        options.ApiUrl.ShouldBe("https://proxy.example.com/");
        var factoryOptions = provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>().Get(WhiteListServiceCollectionExtensions.HttpClientName);
        factoryOptions.HttpMessageHandlerBuilderActions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddOpenUrzednikMFWhiteList_LoggingRegistered_ClientLogsUnderItsTypeName()
    {
        // Arrange
        var loggerProvider = new RecordingLoggerProvider();
        using var provider = BuildProvider(out _, services => services.AddLogging(logging => logging
            .AddProvider(loggerProvider)
            .SetMinimumLevel(LogLevel.Debug)));

        // Act
        await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync("123", Date, TestContext.Current.CancellationToken);

        // Assert
        loggerProvider.Entries.ShouldContain(entry =>
            entry.Category == "OpenUrzednik.MF.WhiteList.WhiteListClient" &&
            entry.Level == LogLevel.Debug &&
            entry.Message == "Validation failed for SearchByNipAsync");
    }

    [Fact]
    public async Task AddOpenUrzednikMFWhiteList_ListenerOnWhiteListSource_RecordsClientSpans()
    {
        // Arrange
        // Other tests may also trace White List calls, so only spans under this test's own parent count.
        using var parentSource = new ActivitySource($"OpenUrzednik.Tests.{Guid.NewGuid():N}");
        var stopped = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source == parentSource || source.Name == WhiteListTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (stopped)
                    stopped.Add(activity);
            },
        };
        ActivitySource.AddActivityListener(listener);
        using var provider = BuildProvider(out _);
        var client = provider.GetRequiredService<IWhiteListClient>();

        // Act
        ActivityTraceId traceId;
        // An explicit W3C context: on .NET Framework a parent would otherwise get a hierarchical id, without a trace id.
        var parentContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        using (var parent = parentSource.StartActivity("test", ActivityKind.Internal, parentContext))
        {
            traceId = parent!.TraceId;
            await client.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);
        }

        // Assert
        List<string> names;
        lock (stopped)
            names = [.. stopped.Where(a => a.TraceId == traceId && a.Source.Name == WhiteListTelemetry.SourceName).Select(a => a.OperationName)];
        names.ShouldBe(["mf_white_list.http.get", "mf_white_list.search.nip"], ignoreOrder: true);
    }

    [Fact]
    public void AddOpenUrzednikMFWhiteList_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => services.AddOpenUrzednikMFWhiteList());

        // Assert
        exception.ParamName.ShouldBe("services");
    }
}
