using System.Diagnostics;
using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Gold;

using Polly;
using Polly.CircuitBreaker;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// AddOpenUrzednikNbp() with Microsoft's resilience handlers (ADR-0004): retries work, and rejections come back as errors.
// End-to-end through the DI registration, so not split into one file per method.
public sealed class NbpDependencyInjectionWireMockTest : IDisposable
{
    private const string Path = "/cenyzlota";
    private const string Scenario = "recovery";
    private const string Recovered = "recovered";

    private readonly WireMockServer _server = WireMockServer.Start(new WireMockServerSettings() { UseSSL = true });
    private ServiceProvider? _provider;

    // configureServices runs before AddOpenUrzednikNbp(), configureHttpClient on the builder it returns.
    private INbpGoldPriceClient CreateSut(Action<IHttpClientBuilder>? configureHttpClient = null, Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        var builder = services.AddOpenUrzednikNbp(options => options.ApiUrl = _server.Urls[0])
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        configureHttpClient?.Invoke(builder);
        _provider = services.BuildServiceProvider();
        return _provider.GetRequiredService<INbpGoldPriceClient>();
    }

    // The first request gets the failure, the next ones the captured gold price.
    private void GivenFailureThenGoldPrice(IResponseBuilder failure)
    {
        _server.Given(Request.Create().WithPath(Path).UsingGet()).InScenario(Scenario).WillSetStateTo(Recovered).RespondWith(failure);
        _server.Given(Request.Create().WithPath(Path).UsingGet()).InScenario(Scenario).WhenStateIs(Recovered)
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(NbpFixtures.Load("gold-latest.json")));
    }

    [Fact]
    public async Task GetLatestAsync_NoResilienceHandler_ReturnsTheFirstFailure()
    {
        // Arrange
        _server.GivenError(Path, HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>()
            .Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(503);
        _server.LogEntries.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GetLatestAsync_StandardHandlerAndOne503_RetriesAndSucceeds()
    {
        // Arrange
        GivenFailureThenGoldPrice(Response.Create().WithStatusCode(HttpStatusCode.ServiceUnavailable));
        var sut = CreateSut(builder => builder.AddStandardResilienceHandler(options => options.Retry.Delay = TimeSpan.FromMilliseconds(10)));

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(new DateOnly(2026, 10, 2), 517.55m));
        _server.LogEntries.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetLatestAsync_StandardHandlerAnd429WithRetryAfter_WaitsAndSucceeds()
    {
        // Arrange
        GivenFailureThenGoldPrice(Response.Create().WithStatusCode(HttpStatusCode.TooManyRequests).WithHeader("Retry-After", "1"));
        var sut = CreateSut(builder => builder.AddStandardResilienceHandler(options => options.Retry.Delay = TimeSpan.FromMilliseconds(10)));
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(0.9));
        _server.LogEntries.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetLatestAsync_StandardHandlerTimesOut_ReturnsRequestTimeoutError()
    {
        // Arrange
        _server.GivenSlowResponse(Path);
        var sut = CreateSut(builder => builder.AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(200);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMilliseconds(500);
            options.Retry.Delay = TimeSpan.FromMilliseconds(10);
        }));
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(NbpWireMockServerExtensions.GaveUpWithin);
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>();
        // The handler's limit isn't HttpClient.Timeout or the request deadline, so the error doesn't name one.
        error.Timeout.ShouldBeNull();
        var cancelled = error.Exception.ShouldBeAssignableTo<OperationCanceledException>().ShouldNotBeNull();
#if NET
        // .NET Framework's HttpClient replaces a cancellation with a new exception, so the rejection is only kept on .NET.
        cancelled.InnerException.ShouldBeOfType<Polly.Timeout.TimeoutRejectedException>();
#endif
    }

    private static void AddCircuitBreaker(IHttpClientBuilder builder)
        => builder.AddResilienceHandler("circuit-breaker", pipeline => pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            MinimumThroughput = 2,
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),
        }));

    public static TheoryData<string> CircuitBreakerPlacements => ["returned builder", "client defaults", "before registration"];

    // The rejection handler must stay outermost wherever the app adds resilience.
    [Theory]
    [MemberData(nameof(CircuitBreakerPlacements))]
    public async Task GetLatestAsync_CircuitBreakerOpen_ReturnsServiceUnavailableErrorWithoutRequest(string placement)
    {
        // Arrange
        _server.GivenError(Path, HttpStatusCode.ServiceUnavailable);
        var sut = placement switch
        {
            "returned builder" => CreateSut(AddCircuitBreaker),
            "client defaults" => CreateSut(configureServices: services => services.ConfigureHttpClientDefaults(AddCircuitBreaker)),
            _ => CreateSut(configureServices: services => AddCircuitBreaker(services.AddHttpClient(NbpServiceCollectionExtensions.HttpClientName))),
        };
        await sut.GetLatestAsync(TestContext.Current.CancellationToken);
        await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Exception.ShouldBeOfType<HttpRequestException>().InnerException.ShouldBeAssignableTo<BrokenCircuitException>();
        _server.LogEntries.Count().ShouldBe(2);
    }

    public void Dispose()
    {
        _provider?.Dispose();
        _server.Dispose();
    }
}
