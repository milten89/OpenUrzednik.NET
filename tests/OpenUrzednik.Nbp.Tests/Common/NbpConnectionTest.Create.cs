using System.Net;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public partial class NbpConnectionTest
{
    private static RestRequestExecutor Create(HttpClient httpClient, NbpOptions? options)
        => NbpConnection.Create(httpClient, options,
            new OpenUrzednikTelemetry(NullOpenUrzednikLogger.Instance, NullOpenUrzednikTraceSource.Instance), new FakeTimeProvider());

    [Fact]
    public void Create_NoOptionsAndNoBaseAddress_UsesDefaultApiUrlAndHttpClientTimeout()
    {
        // Arrange
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(42) };

        // Act
        var connection = Create(httpClient, null);

        // Assert
        connection.BaseAddress.ToString().ShouldBe(NbpOptions.DefaultApiUrl);
        connection.Timeout.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void Create_HttpClientBaseAddressOnly_UsesBaseAddress()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://gateway.example.com/nbp/") };

        // Act
        var connection = Create(httpClient, new NbpOptions());

        // Assert
        connection.BaseAddress.ToString().ShouldBe("https://gateway.example.com/nbp/");
    }

    [Fact]
    public void Create_ApiUrlAndBaseAddress_ApiUrlWins()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://other.example.com/") };

        // Act
        var connection = Create(httpClient, new NbpOptions { ApiUrl = "https://proxy.example.com/nbp" });

        // Assert
        connection.BaseAddress.ToString().ShouldBe("https://proxy.example.com/nbp/");
    }

    [Fact]
    public void Create_Always_LeavesHttpClientUnchanged()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var timeout = httpClient.Timeout;

        // Act
        Create(httpClient, new NbpOptions { ApiUrl = "https://proxy.example.com/", Timeout = TimeSpan.FromSeconds(5) });

        // Assert
        httpClient.BaseAddress.ShouldBeNull();
        httpClient.Timeout.ShouldBe(timeout);
    }

    [Fact]
    public void Create_InvalidApiUrl_ThrowsArgumentExceptionForOptions()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        var exception = Should.Throw<ArgumentException>(() => Create(httpClient, new NbpOptions { ApiUrl = "https://gateway.example.com/api?key=1" }));
        exception.ParamName.ShouldBe("options");
        exception.Message.ShouldStartWith("Invalid NBP API url: https://gateway.example.com/api?key=1.");
    }

    [Fact]
    public void Create_HttpBaseAddress_ThrowsArgumentException()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://api.nbp.pl/api/") };

        // Act && Assert
        var exception = Should.Throw<ArgumentException>(() => Create(httpClient, null));
        exception.ParamName.ShouldBe("httpClient");
        exception.Message.ShouldContain("NBP API supports only HTTPS");
    }

    [Fact]
    public void Create_PositiveTimeout_UsesItAsDeadlineInsteadOfHttpClientTimeout()
    {
        // Arrange
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(100) };

        // Act
        var connection = Create(httpClient, new NbpOptions { Timeout = TimeSpan.FromSeconds(5) });

        // Assert
        connection.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_InvalidTimeout_ThrowsArgumentExceptionForOptions()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        var exception = Should.Throw<ArgumentException>(() => Create(httpClient, new NbpOptions { Timeout = TimeSpan.Zero }));
        exception.ParamName.ShouldBe("options");
        exception.Message.ShouldStartWith("Invalid NBP API timeout: ");
    }

    [Fact]
    public async Task Create_Always_NamesHttpSpanAndErrorsAfterNbp()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(Substitute.For<IOpenUrzednikSpan>());
        var connection = NbpConnection.Create(httpClient, null, new OpenUrzednikTelemetry(NullOpenUrzednikLogger.Instance, tracer), new FakeTimeProvider());

        // Act
        var result = await connection.GetAsync("cenyzlota", NbpJsonContext.Default.GoldPriceDtoArray, TestContext.Current.CancellationToken);

        // Assert
        // The span name is part of the telemetry contract (ADR-0003).
        tracer.Received(1).StartSpan("nbp.http.get");
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("NBP API is unavailable, status 503.");
    }

    [Fact]
    public void Create_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Create(null!, null))
            .ParamName.ShouldBe("httpClient");
    }
}
