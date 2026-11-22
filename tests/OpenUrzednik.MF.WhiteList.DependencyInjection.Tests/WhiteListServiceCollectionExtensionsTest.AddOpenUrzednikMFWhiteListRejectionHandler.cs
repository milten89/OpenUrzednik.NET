using Microsoft.Extensions.DependencyInjection;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Transport.Http;

using Polly.CircuitBreaker;
using Polly.Timeout;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

public sealed partial class WhiteListServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddOpenUrzednikMFWhiteListRejectionHandler_TimeoutRejection_ReturnsRequestTimeoutError()
    {
        // Arrange
        using var provider = BuildOwnClientProvider(new TimeoutRejectedException(), builder => builder.AddOpenUrzednikMFWhiteListRejectionHandler());
        var sut = new WhiteListClient(provider.GetRequiredService<IHttpClientFactory>().CreateClient(OwnClientName));

        // Act
        var result = await sut.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>();
    }

    [Fact]
    public async Task AddOpenUrzednikMFWhiteListRejectionHandler_BrokenCircuit_ReturnsServiceUnavailableError()
    {
        // Arrange
        using var provider = BuildOwnClientProvider(new BrokenCircuitException(), builder => builder.AddOpenUrzednikMFWhiteListRejectionHandler());
        var sut = new WhiteListClient(provider.GetRequiredService<IHttpClientFactory>().CreateClient(OwnClientName));

        // Act
        var result = await sut.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Metadata.ContainsKey(HttpStatusExtensions.StatusCodeMetadataKey).ShouldBeFalse();
    }

    [Fact]
    public async Task AddOpenUrzednikMFWhiteListRejectionHandler_NotAdded_RejectionIsThrown()
    {
        // Arrange
        using var provider = BuildOwnClientProvider(new BrokenCircuitException(), _ => { });
        var sut = new WhiteListClient(provider.GetRequiredService<IHttpClientFactory>().CreateClient(OwnClientName));

        // Act & Assert
        await Should.ThrowAsync<BrokenCircuitException>(() => sut.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void AddOpenUrzednikMFWhiteListRejectionHandler_CalledTwice_AddsOneHandler()
    {
        // Arrange
        using var provider = BuildOwnClientProvider(new BrokenCircuitException(),
            builder => builder.AddOpenUrzednikMFWhiteListRejectionHandler().AddOpenUrzednikMFWhiteListRejectionHandler());

        // Act
        var count = CountRejectionHandlers(provider, OwnClientName);

        // Assert
        count.ShouldBe(1);
    }

    [Fact]
    public void AddOpenUrzednikMFWhiteListRejectionHandler_OnAddOpenUrzednikMFWhiteListBuilder_AddsNoSecondHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOpenUrzednikMFWhiteList().AddOpenUrzednikMFWhiteListRejectionHandler();
        using var provider = services.BuildServiceProvider();

        // Act
        var count = CountRejectionHandlers(provider, WhiteListServiceCollectionExtensions.HttpClientName);

        // Assert
        count.ShouldBe(1);
    }

    [Fact]
    public void AddOpenUrzednikMFWhiteListRejectionHandler_HandlerAddedBefore_StaysOutermost()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient(OwnClientName)
            .AddHttpMessageHandler(() => new PassThroughHandler())
            .AddOpenUrzednikMFWhiteListRejectionHandler();
        using var provider = services.BuildServiceProvider();

        // Act
        var handlers = GetDelegatingHandlers(provider, OwnClientName);

        // Assert
        handlers.Count.ShouldBe(2);
        handlers[0].ShouldBeOfType<ResilienceRejectionHandler>();
        handlers[1].ShouldBeOfType<PassThroughHandler>();
    }

    [Fact]
    public void AddOpenUrzednikMFWhiteListRejectionHandler_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHttpClientBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddOpenUrzednikMFWhiteListRejectionHandler())
            .ParamName.ShouldBe("builder");
    }
}
