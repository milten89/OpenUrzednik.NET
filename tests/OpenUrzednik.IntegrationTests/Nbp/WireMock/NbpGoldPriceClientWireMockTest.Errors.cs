using System.Net;

using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// ADR-0002 error paths. The currency and table clients have the same set.
public partial class NbpGoldPriceClientWireMockTest
{
    [Fact]
    public async Task GetTopCountAsync_Returns400_ReturnsBadRequestErrorWithServerMessage()
    {
        // Arrange
        // The validators stop over-limit requests, so a valid request gets the captured over-limit response.
        _server.GivenError($"{BasePath}/last/10", HttpStatusCode.BadRequest, "error-400-top-count.txt");

        // Act
        var result = await CreateSut().GetTopCountAsync(10, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith(": 400 BadRequest - Przekroczony limit 255 wyników / Maximum size of 255 data series has been exceeded");
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task GetAsync_DateRange_Returns400_ReturnsBadRequestErrorWithServerMessage()
    {
        // Arrange
        // The validators stop over-limit requests, so a valid request gets the captured over-limit response.
        var from = new DateOnly(2026, 9, 28);
        var to = new DateOnly(2026, 9, 30);
        _server.GivenError($"{BasePath}/{from.ToIso()}/{to.ToIso()}", HttpStatusCode.BadRequest, "error-400-date-range.txt");

        // Act
        var result = await CreateSut().GetAsync(from, to, TestContext.Current.CancellationToken);

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
        _server.GivenError($"{BasePath}/{date.ToIso()}", HttpStatusCode.NotFound, "error-404.txt");

        // Act
        var result = await CreateSut().GetAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(404);
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithEmptyArray_ReturnsNotFoundError()
    {
        // Arrange
        _server.GivenJson(BasePath, "[]");

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

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
        _server.GivenError(BasePath, statusCode);

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<UnauthorizedError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetLatestAsync_Returns429WithRetryAfter_ReturnsRateLimitExceededErrorWithDelay()
    {
        // Arrange
        _server.GivenRetryAfter(BasePath, "30");

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

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
    public async Task GetLatestAsync_Returns5xx_ReturnsServiceUnavailableError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenError(BasePath, statusCode);

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetLatestAsync_ConnectionFails_ReturnsServiceUnavailableError()
    {
        // Arrange
        var sut = CreateSut();
        _server.Stop();

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Exception.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithMalformedJson_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson(BasePath, "[{\"data\":\"2026-10-01\",\"cena\":");

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
        error.ToException().ShouldBeOfType<OpenUrzednik.Core.Exceptions.OpenUrzednikSerializationException>();
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithNullItem_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson(BasePath, "[null]");

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }
}
