using System.Net;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Errors;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class NbpGoldPriceClientTest
{
    [Fact]
    public void Ctor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpGoldPriceClient(null!))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void CtorWithOptions_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpGoldPriceClient(null!, new NbpOptions(), Substitute.For<INbpUrlBuilderFactory>(), new FakeTimeProvider()))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public async Task Ctor_HttpClientOnly_SendsRequestsToTheDefaultApiUrl()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler);
        var sut = new NbpGoldPriceClient(httpClient);

        // Act
        await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://api.nbp.pl/api/cenyzlota"));
    }

    [Fact]
    public async Task Ctor_HttpClientOnly_ValidatesDatesAgainstTheSystemClock()
    {
        // Arrange
        // Warsaw's date is the UTC date or the next day, so yesterday (UTC) is always past and the day after tomorrow always future.
        var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastHandler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var pastHttpClient = new HttpClient(pastHandler);
        var futureHandler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var futureHttpClient = new HttpClient(futureHandler);

        // Act
        await new NbpGoldPriceClient(pastHttpClient).GetAsync(utcToday.AddDays(-1), TestContext.Current.CancellationToken);
        var future = await new NbpGoldPriceClient(futureHttpClient).GetAsync(utcToday.AddDays(2), TestContext.Current.CancellationToken);

        // Assert
        pastHandler.Request.ShouldNotBeNull();
        future.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        futureHandler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Ctor_CustomUrlBuilderFactory_SendsRequestsToItsUrls()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns("https://custom.example.com/latest");
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        urlBuilderFactory.GetGoldBuilder().Returns(urlBuilder);
        var sut = new NbpGoldPriceClient(httpClient, urlBuilderFactory: urlBuilderFactory);

        // Act
        await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://custom.example.com/latest"));
    }

    [Fact]
    public async Task Ctor_CustomTimeProvider_ValidatesDatesAgainstIt()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler);
        var sut = new NbpGoldPriceClient(httpClient, timeProvider: new FakeTimeProvider(new DateTimeOffset(2100, 1, 1, 12, 0, 0, TimeSpan.Zero)));
        var futureForTheSystemClock = new DateOnly(2099, 6, 1);

        // Act
        var result = await sut.GetAsync(futureForTheSystemClock, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>(); // from the stub
        handler.Request.ShouldNotBeNull();
    }

    [Fact]
    public async Task Ctor_CustomTelemetry_LogsAndTracesThroughIt()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler);
        var (logger, _, tracer) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(true);
        var sut = new NbpGoldPriceClient(httpClient, logger: logger, traceSource: tracer);

        // Act
        await sut.GetAsync(new DateOnly(1990, 1, 1), TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan(Arg.Any<string>());
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for {operation}", "operation", Arg.Any<string>());
    }

    [Fact]
    public void Ctor_InvalidOptions_ThrowsArgumentException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentException>(() => new NbpGoldPriceClient(httpClient, new NbpOptions { ApiUrl = "http://api.nbp.pl/api/" }))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Ctor_Always_LeavesHttpClientUnchanged()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var timeout = httpClient.Timeout;

        // Act
        _ = new NbpGoldPriceClient(httpClient, new NbpOptions { ApiUrl = "https://proxy.example.com/", Timeout = TimeSpan.FromSeconds(5) });

        // Assert
        httpClient.BaseAddress.ShouldBeNull();
        httpClient.Timeout.ShouldBe(timeout);
    }
}
