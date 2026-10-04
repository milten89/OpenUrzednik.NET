using System.Diagnostics;
using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Table;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// ADR-0002 error paths, the same set as for the gold price client.
public partial class NbpExchangeRateTableClientWireMockTest
{
    [Fact]
    public async Task GetTopCountAsync_Returns400_ReturnsBadRequestErrorWithServerMessage()
    {
        // Arrange
        // The tables endpoint allows fewer results than the client checks for (67 for table A), so the API rejects 68.
        _server.GivenError($"{BasePath}/a/last/68", HttpStatusCode.BadRequest, "error-400-tables-top-count.txt");

        // Act
        var result = await CreateSut().GetTopCountAsync(TableType.A, 68, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith(": 400 BadRequest - Przekroczony limit 67 wyników / Maximum size of 67 data series has been exceeded");
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task GetBuySellAsync_DateRange_Returns400_ReturnsBadRequestErrorWithServerMessage()
    {
        // Arrange
        // The validators stop over-limit requests, so a valid request gets the captured over-limit response.
        var from = new DateOnly(2026, 9, 29);
        var to = new DateOnly(2026, 9, 30);
        _server.GivenError($"{BasePath}/c/{from.ToIso()}/{to.ToIso()}", HttpStatusCode.BadRequest, "error-400-tables-date-range.txt");

        // Act
        var result = await CreateSut().GetBuySellAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith(": 400 BadRequest - Przekroczony limit 93 dni / Limit of 93 days has been exceeded");
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task GetAsync_SpecificDate_Returns404_ReturnsNotFoundError()
    {
        // Arrange
        // The API returns 404 for a day without a publication (here a Sunday).
        var date = new DateOnly(2026, 9, 27);
        _server.GivenError($"{BasePath}/a/{date.ToIso()}", HttpStatusCode.NotFound, "error-404.txt");

        // Act
        var result = await CreateSut().GetAsync(TableType.A, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(404);
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithEmptyArray_ReturnsNotFoundError()
    {
        // Arrange
        _server.GivenJson($"{BasePath}/a", "[]");

        // Act
        var result = await CreateSut().GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
    }

    [Fact]
    public async Task GetBuySellLatestAsync_Returns200WithEmptyArray_ReturnsNotFoundError()
    {
        // Arrange
        _server.GivenJson($"{BasePath}/c", "[]");

        // Act
        var result = await CreateSut().GetBuySellLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetLatestAsync_Returns401Or403_ReturnsUnauthorizedError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenError($"{BasePath}/a", statusCode);

        // Act
        var result = await CreateSut().GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<UnauthorizedError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetLatestAsync_Returns429WithRetryAfter_ReturnsRateLimitExceededErrorWithDelay()
    {
        // Arrange
        _server.GivenRetryAfter($"{BasePath}/a", "30");

        // Act
        var result = await CreateSut().GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RateLimitExceededError>();
        error.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(429);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task GetBuySellLatestAsync_Returns5xx_ReturnsServiceUnavailableError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenError($"{BasePath}/c", statusCode);

        // Act
        var result = await CreateSut().GetBuySellLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetLatestAsync_Timeout_ReturnsRequestTimeoutError()
    {
        // Arrange
        _server.GivenDelay($"{BasePath}/a", TimeSpan.FromSeconds(5));
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await CreateSut(timeout: TimeSpan.FromSeconds(0.1)).GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(TimeSpan.FromSeconds(0.1));
    }

    [Fact]
    public async Task GetLatestAsync_ConnectionFails_ReturnsServiceUnavailableError()
    {
        // Arrange
        var sut = CreateSut();
        _server.Stop();

        // Act
        var result = await sut.GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Exception.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithMalformedJson_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson($"{BasePath}/a", """[{"table":"A","no":"191/A/NBP/2026","effectiveDate":""");

        // Act
        var result = await CreateSut().GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithNullTable_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson($"{BasePath}/a", "[null]");

        // Act
        var result = await CreateSut().GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetBuySellLatestAsync_Returns200WithMidRates_ReturnsSerializationError()
    {
        // Arrange
        // A table A body where a table C body is expected: 'tradingDate', 'bid' and 'ask' are missing.
        _server.GivenFixture($"{BasePath}/c", "tables-a-latest.json");

        // Act
        var result = await CreateSut().GetBuySellLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }
}
