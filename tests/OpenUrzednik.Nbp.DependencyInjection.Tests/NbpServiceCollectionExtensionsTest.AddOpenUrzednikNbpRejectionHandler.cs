using Microsoft.Extensions.DependencyInjection;

using OpenUrzednik.Core.Errors;

using Polly.CircuitBreaker;
using Polly.Timeout;

using Shouldly;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

public sealed partial class NbpServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddOpenUrzednikNbpRejectionHandler_TimeoutRejection_ReturnsRequestTimeoutError()
    {
        // Arrange
        using var provider = BuildOwnClientProvider(new TimeoutRejectedException(), builder => builder.AddOpenUrzednikNbpRejectionHandler());
        var sut = new NbpGoldPriceClient(provider.GetRequiredService<IHttpClientFactory>().CreateClient(OwnClientName));

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>();
    }

    [Fact]
    public async Task AddOpenUrzednikNbpRejectionHandler_BrokenCircuit_ReturnsServiceUnavailableError()
    {
        // Arrange
        using var provider = BuildOwnClientProvider(new BrokenCircuitException(), builder => builder.AddOpenUrzednikNbpRejectionHandler());
        var sut = new NbpGoldPriceClient(provider.GetRequiredService<IHttpClientFactory>().CreateClient(OwnClientName));

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Metadata.ContainsKey(HttpErrorMetadata.StatusCodeKey).ShouldBeFalse();
    }

    [Fact]
    public async Task AddOpenUrzednikNbpRejectionHandler_NotAdded_RejectionIsThrown()
    {
        // Arrange
        using var provider = BuildOwnClientProvider(new BrokenCircuitException(), _ => { });
        var sut = new NbpGoldPriceClient(provider.GetRequiredService<IHttpClientFactory>().CreateClient(OwnClientName));

        // Act & Assert
        await Should.ThrowAsync<BrokenCircuitException>(() => sut.GetLatestAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void AddOpenUrzednikNbpRejectionHandler_CalledTwice_AddsOneHandler()
    {
        // Arrange
        using var provider = BuildOwnClientProvider(new BrokenCircuitException(),
            builder => builder.AddOpenUrzednikNbpRejectionHandler().AddOpenUrzednikNbpRejectionHandler());

        // Act
        var count = CountRejectionHandlers(provider, OwnClientName);

        // Assert
        count.ShouldBe(1);
    }

    [Fact]
    public void AddOpenUrzednikNbpRejectionHandler_OnAddOpenUrzednikNbpBuilder_AddsNoSecondHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOpenUrzednikNbp().AddOpenUrzednikNbpRejectionHandler();
        using var provider = services.BuildServiceProvider();

        // Act
        var count = CountRejectionHandlers(provider, NbpServiceCollectionExtensions.HttpClientName);

        // Assert
        count.ShouldBe(1);
    }

    [Fact]
    public void AddOpenUrzednikNbpRejectionHandler_HandlerAddedBefore_StaysOutermost()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient(OwnClientName)
            .AddHttpMessageHandler(() => new PassThroughHandler())
            .AddOpenUrzednikNbpRejectionHandler();
        using var provider = services.BuildServiceProvider();

        // Act
        var handlers = GetDelegatingHandlers(provider, OwnClientName);

        // Assert
        handlers.Count.ShouldBe(2);
        handlers[0].ShouldBeOfType<ResilienceRejectionHandler>();
        handlers[1].ShouldBeOfType<PassThroughHandler>();
    }

    [Fact]
    public void AddOpenUrzednikNbpRejectionHandler_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHttpClientBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddOpenUrzednikNbpRejectionHandler())
            .ParamName.ShouldBe("builder");
    }
}
