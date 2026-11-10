using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;
using OpenUrzednik.Transport.Http;

using Polly.CircuitBreaker;

using Shouldly;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

public sealed partial class NbpServiceCollectionExtensionsTest
{
    [Fact]
    public void AddKeyedOpenUrzednikNbp_Key_ResolvesEveryClientUnderTheKey()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikNbp("gateway");
        using var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.GetRequiredKeyedService<INbpCurrencyExchangeRateClient>("gateway").ShouldBeOfType<NbpCurrencyExchangeRateClient>();
        provider.GetRequiredKeyedService<INbpExchangeRateTableClient>("gateway").ShouldBeOfType<NbpExchangeRateTableClient>();
        provider.GetRequiredKeyedService<INbpGoldPriceClient>("gateway").ShouldBeOfType<NbpGoldPriceClient>();
        provider.GetRequiredKeyedService<NbpGoldPriceClient>("gateway")
            .ShouldNotBeSameAs(provider.GetRequiredKeyedService<NbpGoldPriceClient>("gateway"));
        provider.GetService<INbpGoldPriceClient>().ShouldBeNull();
    }

    [Fact]
    public void AddKeyedOpenUrzednikNbp_Key_ReturnsBuilderOfTheKeysHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddKeyedOpenUrzednikNbp("gateway");

        // Assert
        builder.Name.ShouldBe(NbpServiceCollectionExtensions.HttpClientName + ":gateway");
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikNbp_TwoKeysAndDefault_EachCallsItsOwnApiUrl()
    {
        // Arrange
        var first = new StubHttpMessageHandler(Json(GoldPriceJson));
        var second = new StubHttpMessageHandler(Json(GoldPriceJson));
        var unkeyed = new StubHttpMessageHandler(Json(GoldPriceJson));
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikNbp("first", options => options.ApiUrl = "https://first.example.com/nbp/")
            .ConfigurePrimaryHttpMessageHandler(() => first);
        services.AddKeyedOpenUrzednikNbp("second", options => options.ApiUrl = "https://second.example.com/nbp/")
            .ConfigurePrimaryHttpMessageHandler(() => second);
        services.AddOpenUrzednikNbp().ConfigurePrimaryHttpMessageHandler(() => unkeyed);
        using var provider = services.BuildServiceProvider();

        // Act
        await provider.GetRequiredKeyedService<INbpGoldPriceClient>("first").GetLatestAsync(TestContext.Current.CancellationToken);
        await provider.GetRequiredKeyedService<INbpGoldPriceClient>("second").GetLatestAsync(TestContext.Current.CancellationToken);
        await provider.GetRequiredService<INbpGoldPriceClient>().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        first.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://first.example.com/nbp/cenyzlota"));
        second.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://second.example.com/nbp/cenyzlota"));
        unkeyed.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://api.nbp.pl/api/cenyzlota"));
    }

    [Fact]
    public void AddKeyedOpenUrzednikNbp_CalledTwiceWithOneKey_RegistersOnceAndAppliesBothConfigurations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikNbp("gateway", options => options.ApiUrl = "https://proxy.example.com/nbp/");
        var registered = services.Count(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "gateway"));
        services.AddKeyedOpenUrzednikNbp("gateway", options => options.Timeout = TimeSpan.FromSeconds(5));
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptionsMonitor<NbpOptions>>().Get("gateway");

        // Assert
        // Nothing under the key is added again: neither the clients nor the key's own registration.
        services.Count(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "gateway")).ShouldBe(registered);
        options.ApiUrl.ShouldBe("https://proxy.example.com/nbp/");
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        CountRejectionHandlers(provider, NbpServiceCollectionExtensions.HttpClientName + ":gateway").ShouldBe(1);
    }

    [Fact]
    public void AddKeyedOpenUrzednikNbp_KeyUsedByAnotherService_RegistersTheClients()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton("gateway", "the app's own service under the same key");
        services.AddKeyedOpenUrzednikNbp("gateway");
        using var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.GetRequiredKeyedService<INbpGoldPriceClient>("gateway").ShouldBeOfType<NbpGoldPriceClient>();
    }

    [Fact]
    public void AddKeyedOpenUrzednikNbp_InvalidOptionsForKey_FailsStartupValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikNbp("gateway", options => options.ApiUrl = "http://api.nbp.pl/api/");
        using var provider = services.BuildServiceProvider();

        // Act
        var exception = Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());

        // Assert
        exception.OptionsName.ShouldBe("gateway");
        exception.Message.ShouldContain("NBP API supports only HTTPS");
    }

    [Fact]
    public void AddKeyedOpenUrzednikNbp_InvalidOptionsForKey_ThrowsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikNbp("gateway", options => options.ApiUrl = "not a url");
        using var provider = services.BuildServiceProvider();

        // Act & Assert
        Should.Throw<OptionsValidationException>(() => provider.GetRequiredKeyedService<INbpGoldPriceClient>("gateway"))
            .OptionsName.ShouldBe("gateway");
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikNbp_OptionsChangedAfterFirstUse_KeepsTheFirstOptions()
    {
        // Arrange
        Uri? lastRequest = null;
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikNbp("gateway", options => options.ApiUrl = "https://first.example.com/nbp/")
            .ConfigurePrimaryHttpMessageHandler(() => new DelegatingStubHttpMessageHandler((request, _) =>
            {
                lastRequest = request.RequestUri;
                return Task.FromResult(Json(GoldPriceJson));
            }));
        using var provider = services.BuildServiceProvider();
        await provider.GetRequiredKeyedService<INbpGoldPriceClient>("gateway").GetLatestAsync(TestContext.Current.CancellationToken);
        var cache = provider.GetRequiredService<IOptionsMonitorCache<NbpOptions>>();
        cache.TryRemove("gateway");
        cache.TryAdd("gateway", new NbpOptions { ApiUrl = "https://second.example.com/nbp/" });

        // Act
        await provider.GetRequiredKeyedService<INbpGoldPriceClient>("gateway").GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        lastRequest.ShouldBe(new Uri("https://first.example.com/nbp/cenyzlota"));
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikNbp_LoggingRegistered_ClientLogsUnderItsTypeName()
    {
        // Arrange
        var loggerProvider = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(loggerProvider).SetMinimumLevel(LogLevel.Debug));
        services.AddKeyedOpenUrzednikNbp("gateway");
        using var provider = services.BuildServiceProvider();

        // Act
        await provider.GetRequiredKeyedService<INbpGoldPriceClient>("gateway").GetTopCountAsync(0, TestContext.Current.CancellationToken);

        // Assert
        loggerProvider.Entries.ShouldContain(entry =>
            entry.Category == "OpenUrzednik.Nbp.NbpGoldPriceClient" &&
            entry.Level == LogLevel.Debug &&
            entry.Message == "Validation failed for GetTopCountAsync");
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikNbp_ResilienceRejection_ReturnsServiceUnavailableError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikNbp("gateway")
            .ConfigurePrimaryHttpMessageHandler(() => new DelegatingStubHttpMessageHandler((_, _) => throw new BrokenCircuitException()));
        using var provider = services.BuildServiceProvider();

        // Act
        var result = await provider.GetRequiredKeyedService<INbpGoldPriceClient>("gateway").GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>().GetHttpStatus().ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddKeyedOpenUrzednikNbp_EmptyKey_ThrowsArgumentException(string serviceKey)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() => services.AddKeyedOpenUrzednikNbp(serviceKey)).ParamName.ShouldBe("serviceKey");
    }

    [Fact]
    public void AddKeyedOpenUrzednikNbp_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddKeyedOpenUrzednikNbp(null!)).ParamName.ShouldBe("serviceKey");
    }

    [Fact]
    public void AddKeyedOpenUrzednikNbp_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddKeyedOpenUrzednikNbp("gateway")).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddKeyedOpenUrzednikNbp_NullServicesAndKey_ThrowsForServices()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddKeyedOpenUrzednikNbp(null!)).ParamName.ShouldBe("services");
    }
}
