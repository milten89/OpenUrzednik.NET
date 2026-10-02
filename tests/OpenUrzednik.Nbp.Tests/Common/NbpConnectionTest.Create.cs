using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.Telemetry;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public partial class NbpConnectionTest
{
    private static NbpConnection Create(HttpClient httpClient, NbpOptions? options)
        => NbpConnection.Create(httpClient, options,
            new NbpTelemetryProvider(NullOpenUrzednikLogger.Instance, NullOpenUrzednikTraceSource.Instance), new FakeTimeProvider());

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

    [Theory]
    [InlineData("https://api.example.com", "https://api.example.com/")]
    [InlineData("https://api.example.com/", "https://api.example.com/")]
    [InlineData("https://api.example.com/v1", "https://api.example.com/v1/")]
    [InlineData("https://api.example.com/v1/", "https://api.example.com/v1/")]
    public void Create_ValidApiUrl_AddsTrailingSlashOnlyWhenMissing(string apiUrl, string expected)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var connection = Create(httpClient, new NbpOptions { ApiUrl = apiUrl });

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
    public void Create_InvalidApiUrl_ThrowsArgumentException(string apiUrl)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentException>(() => Create(httpClient, new NbpOptions { ApiUrl = apiUrl }))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Create_HttpBaseAddress_ThrowsArgumentException()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://api.nbp.pl/api/") };

        // Act && Assert
        var exception = Should.Throw<ArgumentException>(() => Create(httpClient, null));
        exception.ParamName.ShouldBe("httpClient");
        exception.Message.ShouldContain("no longer supports HTTP");
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
    public void Create_InfiniteTimeout_HasNoOwnDeadline()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var connection = Create(httpClient, new NbpOptions { Timeout = Timeout.InfiniteTimeSpan });

        // Assert
        connection.Timeout.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_InvalidTimeout_ThrowsArgumentException(double seconds)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentException>(() => Create(httpClient, new NbpOptions { Timeout = TimeSpan.FromSeconds(seconds) }))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Create_InfiniteHttpClientTimeoutAndNoOption_HasNoDeadline()
    {
        // Arrange
        using var httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        // Act
        var connection = Create(httpClient, null);

        // Assert
        connection.Timeout.ShouldBeNull();
    }

    [Fact]
    public void Create_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Create(null!, null))
            .ParamName.ShouldBe("httpClient");
    }
}
