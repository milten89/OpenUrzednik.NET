using System.Diagnostics;
using System.Net;

using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// ADR-0002 error paths, the same set as for the gold price client.
public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    [Fact]
    public async Task GetTopCountAsync_Returns400_ReturnsBadRequestErrorWithServerMessage()
    {
        // Arrange
        // The validators stop over-limit requests, so a valid request gets the captured over-limit response.
        _server.GivenError($"{BasePath}/a/{CurrencyCode}/last/10", HttpStatusCode.BadRequest, "error-400-top-count.txt");

        // Act
        var result = await CreateSut().GetTopCountAsync(CurrencyCode, 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith(": 400 BadRequest - Przekroczony limit 255 wyników / Maximum size of 255 data series has been exceeded");
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task GetBuySellAsync_DateRange_Returns400_ReturnsBadRequestErrorWithServerMessage()
    {
        // Arrange
        // The validators stop over-limit requests, so a valid request gets the captured over-limit response.
        var from = new DateOnly(2026, 9, 28);
        var to = new DateOnly(2026, 9, 30);
        _server.GivenError($"{BasePath}/c/{CurrencyCode}/{from:O}/{to:O}", HttpStatusCode.BadRequest, "error-400-date-range.txt");

        // Act
        var result = await CreateSut().GetBuySellAsync(CurrencyCode, from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith(": 400 BadRequest - Przekroczony limit 367 dni / Limit of 367 days has been exceeded");
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task GetAsync_SpecificDate_Returns404_ReturnsNotFoundError()
    {
        // Arrange
        // The API returns 404 for a day without a publication (here a Sunday).
        var date = new DateOnly(2026, 9, 27);
        _server.GivenError($"{BasePath}/a/{CurrencyCode}/{date:O}", HttpStatusCode.NotFound, "error-404.txt");

        // Act
        var result = await CreateSut().GetAsync(CurrencyCode, date, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(404);
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithEmptyRates_ReturnsEmptyRates()
    {
        // Arrange
        // Unlike the array endpoints, an object with no rates isn't a NotFoundError. The API itself answers 404.
        _server.GivenJson($"{BasePath}/a/{CurrencyCode}", """{"table":"A","currency":"dolar amerykański","code":"USD","rates":[]}""");

        // Act
        var result = await CreateSut().GetLatestAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Rates.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetLatestAsync_Returns401Or403_ReturnsUnauthorizedError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenError($"{BasePath}/a/{CurrencyCode}", statusCode);

        // Act
        var result = await CreateSut().GetLatestAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<UnauthorizedError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetLatestAsync_Returns429WithRetryAfter_ReturnsRateLimitExceededErrorWithDelay()
    {
        // Arrange
        _server.GivenRetryAfter($"{BasePath}/a/{CurrencyCode}", "30");

        // Act
        var result = await CreateSut().GetLatestAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

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
        _server.GivenError($"{BasePath}/c/{CurrencyCode}", statusCode);

        // Act
        var result = await CreateSut().GetBuySellLatestAsync(CurrencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetLatestAsync_Timeout_ReturnsRequestTimeoutError()
    {
        // Arrange
        _server.GivenDelay($"{BasePath}/a/{CurrencyCode}", TimeSpan.FromSeconds(5));
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await CreateSut(timeout: TimeSpan.FromSeconds(0.1))
            .GetLatestAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

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
        var result = await sut.GetLatestAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Exception.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithMalformedJson_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson($"{BasePath}/a/{CurrencyCode}", """{"table":"A","currency":"dolar amerykański","code":"USD","rates":[{"no":""");

        // Act
        var result = await CreateSut().GetLatestAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithNullRate_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson($"{BasePath}/a/{CurrencyCode}", """{"table":"A","currency":"dolar amerykański","code":"USD","rates":[null]}""");

        // Act
        var result = await CreateSut().GetLatestAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetBuySellLatestAsync_Returns200WithMidRates_ReturnsSerializationError()
    {
        // Arrange
        // A table A body where a table C body is expected: 'bid' and 'ask' are missing.
        _server.GivenFixture($"{BasePath}/c/{CurrencyCode}", "rates-a-latest.json");

        // Act
        var result = await CreateSut().GetBuySellLatestAsync(CurrencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }
}
