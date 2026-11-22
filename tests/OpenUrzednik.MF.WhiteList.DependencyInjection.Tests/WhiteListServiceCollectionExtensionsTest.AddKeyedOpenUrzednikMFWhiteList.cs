using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;
using OpenUrzednik.Transport.Http;

using Polly.CircuitBreaker;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

public sealed partial class WhiteListServiceCollectionExtensionsTest
{
    [Fact]
    public void AddKeyedOpenUrzednikMFWhiteList_Key_ResolvesEveryClientUnderTheKey()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikMFWhiteList("gateway");
        using var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.GetRequiredKeyedService<IWhiteListStatusClient>("gateway").ShouldBeOfType<WhiteListStatusClient>();
        provider.GetRequiredKeyedService<IWhiteListClient>("gateway").ShouldBeOfType<WhiteListClient>();
        provider.GetRequiredKeyedService<WhiteListClient>("gateway")
            .ShouldNotBeSameAs(provider.GetRequiredKeyedService<WhiteListClient>("gateway"));
        provider.GetService<IWhiteListClient>().ShouldBeNull();
    }

    [Fact]
    public void AddKeyedOpenUrzednikMFWhiteList_Key_ReturnsBuilderOfTheKeysHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddKeyedOpenUrzednikMFWhiteList("gateway");

        // Assert
        builder.Name.ShouldBe(WhiteListServiceCollectionExtensions.HttpClientName + ":gateway");
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikMFWhiteList_TwoKeysAndDefault_EachCallsItsOwnApiUrl()
    {
        // Arrange
        var first = new StubHttpMessageHandler(Json(SearchNipJson));
        var second = new StubHttpMessageHandler(Json(SearchNipJson));
        var unkeyed = new StubHttpMessageHandler(Json(SearchNipJson));
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikMFWhiteList("first", options => options.ApiUrl = "https://first.example.com/")
            .ConfigurePrimaryHttpMessageHandler(() => first);
        services.AddKeyedOpenUrzednikMFWhiteList("second", options => options.ApiUrl = "https://second.example.com/")
            .ConfigurePrimaryHttpMessageHandler(() => second);
        services.AddOpenUrzednikMFWhiteList().ConfigurePrimaryHttpMessageHandler(() => unkeyed);
        using var provider = services.BuildServiceProvider();

        // Act
        await provider.GetRequiredKeyedService<IWhiteListClient>("first").SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);
        await provider.GetRequiredKeyedService<IWhiteListClient>("second").SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);
        await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        first.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://first.example.com/api/search/nip/3245174504?date=2026-11-02"));
        second.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://second.example.com/api/search/nip/3245174504?date=2026-11-02"));
        unkeyed.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/3245174504?date=2026-11-02"));
    }

    [Fact]
    public void AddKeyedOpenUrzednikMFWhiteList_CalledTwiceWithOneKey_RegistersOnceAndAppliesBothConfigurations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikMFWhiteList("gateway", options => options.ApiUrl = "https://proxy.example.com/");
        var registered = services.Count(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "gateway"));
        services.AddKeyedOpenUrzednikMFWhiteList("gateway", options => options.Timeout = TimeSpan.FromSeconds(5));
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptionsMonitor<WhiteListOptions>>().Get("gateway");

        // Assert
        // Nothing under the key is added again: neither the clients nor the key's own registration.
        services.Count(descriptor => descriptor.IsKeyedService && Equals(descriptor.ServiceKey, "gateway")).ShouldBe(registered);
        options.ApiUrl.ShouldBe("https://proxy.example.com/");
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        CountRejectionHandlers(provider, WhiteListServiceCollectionExtensions.HttpClientName + ":gateway").ShouldBe(1);
    }

    [Fact]
    public void AddKeyedOpenUrzednikMFWhiteList_KeyUsedByAnotherService_RegistersTheClients()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingleton("gateway", "the app's own service under the same key");
        services.AddKeyedOpenUrzednikMFWhiteList("gateway");
        using var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.GetRequiredKeyedService<IWhiteListClient>("gateway").ShouldBeOfType<WhiteListClient>();
    }

    [Fact]
    public void AddKeyedOpenUrzednikMFWhiteList_InvalidOptionsForKey_FailsStartupValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikMFWhiteList("gateway", options => options.ApiUrl = "http://wykazpodatnikowvat.mf.gov.pl/");
        using var provider = services.BuildServiceProvider();

        // Act
        var exception = Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());

        // Assert
        exception.OptionsName.ShouldBe("gateway");
        exception.Message.ShouldContain("supports only HTTPS");
    }

    [Fact]
    public void AddKeyedOpenUrzednikMFWhiteList_InvalidOptionsForKey_ThrowsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikMFWhiteList("gateway", options => options.ApiUrl = "not a url");
        using var provider = services.BuildServiceProvider();

        // Act & Assert
        Should.Throw<OptionsValidationException>(() => provider.GetRequiredKeyedService<IWhiteListClient>("gateway"))
            .OptionsName.ShouldBe("gateway");
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikMFWhiteList_OptionsChangedAfterFirstUse_KeepsTheFirstOptions()
    {
        // Arrange
        Uri? lastRequest = null;
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikMFWhiteList("gateway", options => options.ApiUrl = "https://first.example.com/")
            .ConfigurePrimaryHttpMessageHandler(() => new DelegatingStubHttpMessageHandler((request, _) =>
            {
                lastRequest = request.RequestUri;
                return Task.FromResult(Json(SearchNipJson));
            }));
        using var provider = services.BuildServiceProvider();
        await provider.GetRequiredKeyedService<IWhiteListClient>("gateway").SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);
        var cache = provider.GetRequiredService<IOptionsMonitorCache<WhiteListOptions>>();
        cache.TryRemove("gateway");
        cache.TryAdd("gateway", new WhiteListOptions { ApiUrl = "https://second.example.com/" });

        // Act
        await provider.GetRequiredKeyedService<IWhiteListClient>("gateway").SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        lastRequest.ShouldBe(new Uri("https://first.example.com/api/search/nip/3245174504?date=2026-11-02"));
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikMFWhiteList_LoggingRegistered_ClientLogsUnderItsTypeName()
    {
        // Arrange
        var loggerProvider = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(loggerProvider).SetMinimumLevel(LogLevel.Debug));
        services.AddKeyedOpenUrzednikMFWhiteList("gateway");
        using var provider = services.BuildServiceProvider();

        // Act
        await provider.GetRequiredKeyedService<IWhiteListClient>("gateway").SearchByNipAsync("123", Date, TestContext.Current.CancellationToken);

        // Assert
        loggerProvider.Entries.ShouldContain(entry =>
            entry.Category == "OpenUrzednik.MF.WhiteList.WhiteListClient" &&
            entry.Level == LogLevel.Debug &&
            entry.Message == "Validation failed for SearchByNipAsync");
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikMFWhiteList_ResilienceRejection_ReturnsServiceUnavailableError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikMFWhiteList("gateway")
            .ConfigurePrimaryHttpMessageHandler(() => new DelegatingStubHttpMessageHandler((_, _) => throw new BrokenCircuitException()));
        using var provider = services.BuildServiceProvider();

        // Act
        var result = await provider.GetRequiredKeyedService<IWhiteListClient>("gateway").SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>().GetHttpStatus().ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddKeyedOpenUrzednikMFWhiteList_EmptyKey_ThrowsArgumentException(string serviceKey)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() => services.AddKeyedOpenUrzednikMFWhiteList(serviceKey)).ParamName.ShouldBe("serviceKey");
    }

    [Fact]
    public void AddKeyedOpenUrzednikMFWhiteList_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddKeyedOpenUrzednikMFWhiteList(null!)).ParamName.ShouldBe("serviceKey");
    }

    [Fact]
    public void AddKeyedOpenUrzednikMFWhiteList_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddKeyedOpenUrzednikMFWhiteList("gateway")).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddKeyedOpenUrzednikMFWhiteList_NullServicesAndKey_ThrowsForServices()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddKeyedOpenUrzednikMFWhiteList(null!)).ParamName.ShouldBe("services");
    }
}
