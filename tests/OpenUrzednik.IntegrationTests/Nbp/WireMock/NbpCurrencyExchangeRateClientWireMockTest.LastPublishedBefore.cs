using System.Diagnostics;
using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Table;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// GetLastPublishedBeforeAsync and GetBuySellLastPublishedBeforeAsync: the success paths and the ADR-0002 error paths.
// The error tests run for both methods ("a" for the mid rates, "c" for buy and sell); they share the request pipeline.
public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    // The 14 days before 2026-10-01 for tables A and C.
    private static readonly DateTime LastBeforeDate = new(2026, 10, 1);

    private static string LastBeforePath(string table) => $"{BasePath}/{table}/{CurrencyCode}/2026-09-17/2026-09-30";

    private static async Task<IReadOnlyList<OpenUrzednikError>> GetLastPublishedBeforeErrorsAsync(NbpCurrencyExchangeRateClient sut, string table)
        => table == "c"
            ? (await sut.GetBuySellLastPublishedBeforeAsync(CurrencyCode, LastBeforeDate, TestContext.Current.CancellationToken)).Errors
            : (await sut.GetLastPublishedBeforeAsync(CurrencyCode, LastBeforeDate, cancellationToken: TestContext.Current.CancellationToken)).Errors;

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Returns200WithData_MapsToLatestRate()
    {
        // Arrange
        var path = LastBeforePath("a");
        _server.GivenFixture(path, "rates-a-range.json");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(CurrencyCode, LastBeforeDate, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyRate("190/A/NBP/2026", new DateTime(2026, 9, 30), 3.8449m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_TableB_Returns200WithData_MapsToLatestRate()
    {
        // Arrange
        // 28 days before 2026-09-24 for table B, published weekly.
        var path = $"{BasePath}/b/{TableBCurrencyCode}/2026-08-27/2026-09-23";
        _server.GivenFixture(path, "rates-b-range.json");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(TableBCurrencyCode, new DateTime(2026, 9, 24), TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(TableBCurrencyName, TableBCurrencyCode, [
            new CurrencyRate("038/B/NBP/2026", new DateTime(2026, 9, 23), 0.058927m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_Returns200WithData_MapsToLatestRate()
    {
        // Arrange
        var path = LastBeforePath("c");
        _server.GivenFixture(path, "rates-c-range.json");

        // Act
        var result = await CreateSut().GetBuySellLastPublishedBeforeAsync(CurrencyCode, LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyBuySellRate("190/C/NBP/2026", new DateTime(2026, 9, 30), Ask: 3.8910m, Bid: 3.8140m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task GetLastPublishedBeforeAsync_Returns400_ReturnsBadRequestErrorWithServerMessage(string table)
    {
        // Arrange
        // The window is always within the API's limits, so a valid request gets the captured over-limit response.
        _server.GivenError(LastBeforePath(table), HttpStatusCode.BadRequest, "error-400-date-range.txt");

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(), table);

        // Assert
        var error = errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith(": 400 BadRequest - Przekroczony limit 367 dni / Limit of 367 days has been exceeded");
        error.Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe(400);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task GetLastPublishedBeforeAsync_Returns404_ReturnsNotFoundError(string table)
    {
        // Arrange
        // The API returns 404 for a range without a publication.
        _server.GivenError(LastBeforePath(table), HttpStatusCode.NotFound, "error-404.txt");

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(), table);

        // Assert
        errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>().Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe(404);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task GetLastPublishedBeforeAsync_Returns200WithEmptyRates_ReturnsNotFoundError(string table)
    {
        // Arrange
        // Unlike the range methods, which return the empty list, there is no rate to return. The API itself answers 404.
        _server.GivenJson(LastBeforePath(table), $$"""{"table":"{{table.ToUpperInvariant()}}","currency":"dolar amerykański","code":"USD","rates":[]}""");

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(), table);

        // Assert
        errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>().Metadata.ContainsKey(HttpErrorMetadata.StatusCodeKey).ShouldBeFalse();
    }

    [Theory]
    [InlineData("a", HttpStatusCode.Unauthorized)]
    [InlineData("c", HttpStatusCode.Forbidden)]
    public async Task GetLastPublishedBeforeAsync_Returns401Or403_ReturnsUnauthorizedError(string table, HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenError(LastBeforePath(table), statusCode);

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(), table);

        // Assert
        errors.ShouldHaveSingleItem().ShouldBeOfType<UnauthorizedError>().Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe((int)statusCode);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task GetLastPublishedBeforeAsync_Returns429WithRetryAfter_ReturnsRateLimitExceededErrorWithDelay(string table)
    {
        // Arrange
        _server.GivenRetryAfter(LastBeforePath(table), "30");

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(), table);

        // Assert
        var error = errors.ShouldHaveSingleItem().ShouldBeOfType<RateLimitExceededError>();
        error.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        error.Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe(429);
    }

    [Theory]
    [InlineData("a", HttpStatusCode.InternalServerError)]
    [InlineData("c", HttpStatusCode.ServiceUnavailable)]
    public async Task GetLastPublishedBeforeAsync_Returns5xx_ReturnsServiceUnavailableError(string table, HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenError(LastBeforePath(table), statusCode);

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(), table);

        // Assert
        errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>().Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe((int)statusCode);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task GetLastPublishedBeforeAsync_Returns200WithMalformedJson_ReturnsSerializationError(string table)
    {
        // Arrange
        _server.GivenJson(LastBeforePath(table), """{"table":"A","currency":"dolar amerykański","code":"USD","rates":[{"no":""");

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(), table);

        // Assert
        errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task GetLastPublishedBeforeAsync_Timeout_ReturnsRequestTimeoutError(string table)
    {
        // Arrange
        _server.GivenSlowResponse(LastBeforePath(table));
        var stopwatch = Stopwatch.StartNew();

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(timeout: TimeSpan.FromSeconds(0.1)), table);

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(NbpWireMockServerExtensions.GaveUpWithin);
        errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(TimeSpan.FromSeconds(0.1));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task GetLastPublishedBeforeAsync_ConnectionFails_ReturnsConnectionFailedError(string table)
    {
        // Arrange
        var sut = CreateSut();
        _server.Stop();

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(sut, table);

        // Assert
        errors.ShouldHaveSingleItem().ShouldBeOfType<ConnectionFailedError>().Exception.ShouldBeOfType<HttpRequestException>();
    }
}
