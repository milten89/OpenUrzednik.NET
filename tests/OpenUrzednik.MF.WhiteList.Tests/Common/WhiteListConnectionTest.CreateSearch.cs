using System.Net;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed partial class WhiteListConnectionTest
{
    [Fact]
    public void CreateSearch_NoOptionsAndNoBaseAddress_UsesProductionApiAndHttpClientTimeout()
    {
        // Arrange
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(42) };

        // Act
        var connection = CreateSearch(httpClient, null);

        // Assert
        connection.BaseAddress.ToString().ShouldBe("https://wykazpodatnikowvat.mf.gov.pl/api/");
        connection.Timeout.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void CreateSearch_TestApiUrl_UsesTestHostApi()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var connection = CreateSearch(httpClient, new WhiteListOptions { ApiUrl = WhiteListOptions.TestApiUrl });

        // Assert
        connection.BaseAddress.ToString().ShouldBe("https://wykazpodatnikowvat-test.mf.gov.pl/api/");
    }

    [Fact]
    public void CreateSearch_HttpClientBaseAddressOnly_UsesBaseAddressAsRoot()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://gateway.example.com/white-list/") };

        // Act
        var connection = CreateSearch(httpClient, new WhiteListOptions());

        // Assert
        connection.BaseAddress.ToString().ShouldBe("https://gateway.example.com/white-list/api/");
    }

    [Fact]
    public void CreateSearch_ApiUrlAndBaseAddress_ApiUrlWins()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://other.example.com/") };

        // Act
        var connection = CreateSearch(httpClient, new WhiteListOptions { ApiUrl = "https://proxy.example.com/white-list" });

        // Assert
        connection.BaseAddress.ToString().ShouldBe("https://proxy.example.com/white-list/api/");
    }

    [Fact]
    public void CreateSearch_Always_LeavesHttpClientUnchanged()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var timeout = httpClient.Timeout;

        // Act
        CreateSearch(httpClient, new WhiteListOptions { ApiUrl = "https://proxy.example.com/", Timeout = TimeSpan.FromSeconds(5) });

        // Assert
        httpClient.BaseAddress.ShouldBeNull();
        httpClient.Timeout.ShouldBe(timeout);
    }

    [Theory]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/api/")]
    [InlineData("https://gateway.example.com/white-list/API2")]
    public void CreateSearch_BaseAddressEndingWithAnApiPath_ThrowsArgumentExceptionAskingForTheHostRoot(string baseAddress)
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };

        // Act && Assert
        var exception = Should.Throw<ArgumentException>(() => CreateSearch(httpClient, null));
        exception.ParamName.ShouldBe("httpClient");
        exception.Message.ShouldContain("Set the host root");
    }

    [Fact]
    public void CreateSearch_ApiUrlEndingWithAnApiPath_ThrowsArgumentExceptionForOptions()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentException>(() => CreateSearch(httpClient, new WhiteListOptions { ApiUrl = "https://wykazpodatnikowvat.mf.gov.pl/api2/" }))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void CreateSearch_PositiveTimeout_UsesItAsDeadlineInsteadOfHttpClientTimeout()
    {
        // Arrange
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(100) };

        // Act
        var connection = CreateSearch(httpClient, new WhiteListOptions { Timeout = TimeSpan.FromSeconds(5) });

        // Assert
        connection.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateSearch_InvalidTimeout_ThrowsArgumentExceptionForOptions()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        var exception = Should.Throw<ArgumentException>(() => CreateSearch(httpClient, new WhiteListOptions { Timeout = TimeSpan.Zero }));
        exception.ParamName.ShouldBe("options");
        exception.Message.ShouldStartWith("Invalid MF White List API timeout: ");
    }

    [Fact]
    public async Task CreateSearch_Always_NamesHttpSpanAndErrorsAfterMfWhiteList()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(Substitute.For<IOpenUrzednikSpan>());
        var connection = WhiteListConnection.CreateSearch(httpClient, null, new OpenUrzednikTelemetry(NullOpenUrzednikLogger.Instance, tracer), new FakeTimeProvider());

        // Act
        var result = await connection.GetAsync("search/nip/1234567890", "search/nip/{nip}", WhiteListJsonContext.Default.ErrorDto,
            TestContext.Current.CancellationToken);

        // Assert
        // The span name is part of the telemetry contract (ADR-0003, ADR-0023).
        tracer.Received(1).StartSpan("mf_white_list.http.get");
        result.Errors.ShouldHaveSingleItem().Message
            .ShouldBe("MF White List API is unavailable for https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/{nip}, status 503.");
    }

    [Fact]
    public void CreateSearch_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => CreateSearch(null!, null))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void CreateSearch_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => WhiteListConnection.CreateSearch(httpClient, null, Telemetry(), null!))
            .ParamName.ShouldBe("timeProvider");
    }
}
