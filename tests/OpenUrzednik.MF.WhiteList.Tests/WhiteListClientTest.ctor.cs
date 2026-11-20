using System.Net;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListClientTest
{
    [Fact]
    public void Ctor_NullHttpClient_Throws()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => new WhiteListClient(null!));

        // Assert
        exception.ParamName.ShouldBe("httpClient");
    }

    [Theory]
    [InlineData("http://wykazpodatnikowvat.mf.gov.pl/")]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/api/")]
    public void Ctor_InvalidApiUrl_Throws(string apiUrl)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act & Assert
        Should.Throw<ArgumentException>(() => new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = apiUrl }));
    }

    [Fact]
    public async Task Ctor_HttpClientOnly_SendsToTheProductionHost()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(Fixture("search-nip.json"));
        using var httpClient = new HttpClient(handler);
        var sut = new WhiteListClient(httpClient);

        // Act
        await sut.SearchByNipAsync(Nip, DateTime.Today.AddDays(-1), TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri!.GetLeftPart(UriPartial.Path).ShouldBe($"https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/{Nip}");
    }

    [Fact]
    public async Task Ctor_TestApiUrl_SendsToTheTestHost()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(Fixture("search-nip.json"));
        using var httpClient = new HttpClient(handler);
        var sut = new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = WhiteListOptions.TestApiUrl });

        // Act
        await sut.SearchByNipAsync(Nip, DateTime.Today.AddDays(-1), TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri!.GetLeftPart(UriPartial.Path).ShouldBe($"https://wykazpodatnikowvat-test.mf.gov.pl/api/search/nip/{Nip}");
    }

    [Fact]
    public async Task Ctor_TimeProvider_DecidesToday()
    {
        // Arrange: 23:30 on 2026-11-20 in UTC is already 2026-11-21 in Warsaw, so 2026-11-21 isn't in the future there
        var handler = new StubHttpMessageHandler(Fixture("search-nip.json"));
        using var httpClient = new HttpClient(handler);
        var clock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 23, 30, 0, TimeSpan.Zero));
        var sut = new WhiteListClient(httpClient, null, clock);

        // Act
        var result = await sut.SearchByNipAsync(Nip, new DateTime(2026, 11, 21), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Ctor_TelemetryWithLogger_LogsThroughIt()
    {
        // Arrange
        var (logger, _, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK), out _, logger, tracer);

        // Act
        await sut.SearchByNipAsync(Nip, Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        logger.Received().Log(OpenUrzednikLogLevel.Debug, MF.WhiteList.Common.WhiteListEvents.ValidationFailed, null, "Validation failed for {operation}",
            "operation", "SearchByNipAsync");
    }
}
