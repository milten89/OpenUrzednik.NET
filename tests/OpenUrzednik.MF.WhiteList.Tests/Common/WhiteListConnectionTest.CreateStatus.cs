using System.Net;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed partial class WhiteListConnectionTest
{
    [Fact]
    public void CreateStatus_NoOptionsAndNoBaseAddress_UsesProductionApi2()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var connection = CreateStatus(httpClient, null);

        // Assert
        connection.BaseAddress.ToString().ShouldBe("https://wykazpodatnikowvat.mf.gov.pl/api2/");
    }

    [Theory]
    [InlineData(WhiteListOptions.TestApiUrl, "https://wykazpodatnikowvat-test.mf.gov.pl/api2/")]
    [InlineData("https://proxy.example.com/white-list", "https://proxy.example.com/white-list/api2/")]
    public void CreateStatus_ApiUrl_AddsApi2ToTheRoot(string apiUrl, string expected)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var connection = CreateStatus(httpClient, new WhiteListOptions { ApiUrl = apiUrl });

        // Assert
        connection.BaseAddress.ToString().ShouldBe(expected);
    }

    [Fact]
    public void CreateStatus_HttpClientBaseAddressOnly_UsesBaseAddressAsRoot()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://gateway.example.com/white-list") };

        // Act
        var connection = CreateStatus(httpClient, null);

        // Assert
        connection.BaseAddress.ToString().ShouldBe("https://gateway.example.com/white-list/api2/");
    }

    [Fact]
    public void CreateStatus_Timeout_UsesItAsDeadline()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var connection = CreateStatus(httpClient, new WhiteListOptions { Timeout = TimeSpan.FromSeconds(7) });

        // Assert
        connection.Timeout.ShouldBe(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task CreateStatus_ErrorResponse_MapsItWithTheClientsClock()
    {
        // Arrange
        var response = new HttpResponseMessage((HttpStatusCode)429)
        {
            Content = new StringContent("""{"message":"Limit zapytań został wyczerpany.","code":"WL-191"}""", Encoding.UTF8, "application/json"),
        };
        using var httpClient = new HttpClient(new StubHttpMessageHandler(response));
        // 23:00 in Warsaw (CET).
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 10, 22, 0, 0, TimeSpan.Zero));
        var connection = WhiteListConnection.CreateStatus(httpClient, null, new OpenUrzednikTelemetry(), timeProvider);

        // Act
        var result = await connection.GetAsync("status/nip/1234567890", "status/nip/{nip}", WhiteListJsonContext.Default.ErrorDto,
            TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(1));
        error.WhiteListCode.ShouldBe("WL-191");
        error.Message.ShouldBe("MF White List API rate limit hit on https://wykazpodatnikowvat.mf.gov.pl/api2/status/nip/{nip}: WL-191 Limit zapytań został wyczerpany.");
    }
}
