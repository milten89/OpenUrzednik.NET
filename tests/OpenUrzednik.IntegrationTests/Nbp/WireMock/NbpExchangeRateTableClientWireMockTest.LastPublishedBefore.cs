using System.Diagnostics;
using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Table;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// GetLastPublishedBeforeAsync and GetBuySellLastPublishedBeforeAsync: the success paths and the ADR-0002 error paths.
// The error tests run for both methods ("a" for the mid tables, "c" for buy and sell); they share the request pipeline.
public partial class NbpExchangeRateTableClientWireMockTest
{
    // The 14 days before 2026-10-01 for tables A and C.
    private static readonly DateTime LastBeforeDate = new(2026, 10, 1);

    private static string LastBeforePath(string table) => $"{BasePath}/{table}/2026-09-17/2026-09-30";

    private static async Task<IReadOnlyList<OpenUrzednikError>> GetLastPublishedBeforeErrorsAsync(NbpExchangeRateTableClient sut, string table)
        => table == "c"
            ? (await sut.GetBuySellLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken)).Errors
            : (await sut.GetLastPublishedBeforeAsync(TableType.A, LastBeforeDate, TestContext.Current.CancellationToken)).Errors;

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Returns200WithData_MapsToLatestTable()
    {
        // Arrange
        var path = LastBeforePath("a");
        _server.GivenFixture(path, "tables-a-range.json");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(TableType.A, LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Table190A);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_TableB_Returns200WithData_MapsToLatestTable()
    {
        // Arrange
        // 28 days for table B, published weekly: the captured answer holds the four tables 036/B to 039/B.
        var path = $"{BasePath}/b/2026-09-03/2026-09-30";
        _server.GivenFixture(path, "tables-b-range.json");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(TableType.B, LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TableId.ShouldBe("039/B/NBP/2026");
        result.Value.PublicationDate.ShouldBe(new DateTime(2026, 9, 30));
        result.Value.Rates.Count.ShouldBe(116);
        result.Value.Rates[0].CurrencyCode.ShouldBe("AFN");
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_Returns200WithData_MapsToLatestTable()
    {
        // Arrange
        var path = LastBeforePath("c");
        _server.GivenFixture(path, "tables-c-range.json");

        // Act
        var result = await CreateSut().GetBuySellLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Table190C);
        _server.ShouldHaveReceivedGet(path);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task GetLastPublishedBeforeAsync_Returns400_ReturnsBadRequestErrorWithServerMessage(string table)
    {
        // Arrange
        // The window is always within the API's limits, so a valid request gets the captured over-limit response.
        _server.GivenError(LastBeforePath(table), HttpStatusCode.BadRequest, "error-400-tables-date-range.txt");

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(), table);

        // Assert
        var error = errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith(": 400 BadRequest - Przekroczony limit 93 dni / Limit of 93 days has been exceeded");
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
    public async Task GetLastPublishedBeforeAsync_Returns200WithEmptyArray_ReturnsNotFoundError(string table)
    {
        // Arrange
        _server.GivenJson(LastBeforePath(table), "[]");

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
    [InlineData("a", HttpStatusCode.BadGateway)]
    [InlineData("c", HttpStatusCode.GatewayTimeout)]
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
        _server.GivenJson(LastBeforePath(table), """[{"table":"A","no":"190/A/NBP/2026","effectiveDate":""");

        // Act
        var errors = await GetLastPublishedBeforeErrorsAsync(CreateSut(), table);

        // Assert
        errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task GetLastPublishedBeforeAsync_Returns200WithNullTable_ReturnsSerializationError(string table)
    {
        // Arrange
        _server.GivenJson(LastBeforePath(table), "[null]");

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
