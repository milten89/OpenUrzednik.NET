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
    [InlineData("https://api.example.com", "https://api.example.com/api/")]
    [InlineData("https://api.example.com/", "https://api.example.com/api/")]
    [InlineData("https://api.example.com/v1", "https://api.example.com/v1/api/")]
    [InlineData("https://api.example.com/v1/", "https://api.example.com/v1/api/")]
    public void CreateSearch_ValidApiUrl_AddsTrailingSlashOnlyWhenMissing(string apiUrl, string expected)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var connection = CreateSearch(httpClient, new WhiteListOptions { ApiUrl = apiUrl });

        // Assert
        connection.BaseAddress.ToString().ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("api.example.com")]
    [InlineData("www.example.com/api")]
    [InlineData("/api/v1/")]
    [InlineData("ftp://example.com/")]
    [InlineData("http://example.com/")]
    [InlineData("https://")]
    [InlineData("https:/example.com")]
    [InlineData("htt ps://example.com")]
    [InlineData("https://example.com:abc/")]
    [InlineData("https://exa mple.com/")]
    [InlineData("https://gateway.example.com/api?key=1")]
    [InlineData("https://gateway.example.com/api/#fragment")]
    [InlineData("https://gateway.example.com/white-list?key=1")]
    [InlineData("https://gateway.example.com/white-list/#fragment")]
    public void CreateSearch_InvalidApiUrl_ThrowsArgumentException(string apiUrl)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentException>(() => CreateSearch(httpClient, new WhiteListOptions { ApiUrl = apiUrl }))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void CreateSearch_HttpBaseAddress_ThrowsArgumentException()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://wykazpodatnikowvat.mf.gov.pl/") };

        // Act && Assert
        var exception = Should.Throw<ArgumentException>(() => CreateSearch(httpClient, null));
        exception.ParamName.ShouldBe("httpClient");
        exception.Message.ShouldContain("supports only HTTPS");
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
    public void CreateSearch_InfiniteTimeout_HasNoOwnDeadline()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var connection = CreateSearch(httpClient, new WhiteListOptions { Timeout = Timeout.InfiniteTimeSpan });

        // Assert
        connection.Timeout.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(60 * 60 * 24 * 60)] // more than int.MaxValue ms, which CancelAfter rejects
    public void CreateSearch_InvalidTimeout_ThrowsArgumentException(double seconds)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentException>(() => CreateSearch(httpClient, new WhiteListOptions { Timeout = TimeSpan.FromSeconds(seconds) }))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void CreateSearch_InfiniteHttpClientTimeoutAndNoOption_HasNoDeadline()
    {
        // Arrange
        using var httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        // Act
        var connection = CreateSearch(httpClient, null);

        // Assert
        connection.Timeout.ShouldBeNull();
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
