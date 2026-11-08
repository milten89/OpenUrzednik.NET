using System.Diagnostics;
using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// GetLastPublishedBeforeAsync: the success path and the ADR-0002 error paths. The request is the 14 days before the date.
public partial class NbpGoldPriceClientWireMockTest
{
    private static readonly DateTime LastBeforeDate = new(2026, 10, 1);
    private const string LastBeforePath = $"{BasePath}/2026-09-17/2026-09-30";

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Returns200WithData_MapsToLatestGoldPrice()
    {
        // Arrange
        _server.GivenFixture(LastBeforePath, "gold-range.json");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(new DateTime(2026, 9, 30), 512.85m));
        _server.ShouldHaveReceivedGet(LastBeforePath);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Returns400_ReturnsBadRequestErrorWithServerMessage()
    {
        // Arrange
        // The window is always within the API's limits, so a valid request gets the captured over-limit response.
        _server.GivenError(LastBeforePath, HttpStatusCode.BadRequest, "error-400-date-range.txt");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpBadRequestError>();
        error.Message.ShouldEndWith(": 400 BadRequest - Przekroczony limit 367 dni / Limit of 367 days has been exceeded");
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Returns404_ReturnsNotFoundError()
    {
        // Arrange
        // The API returns 404 for a range without a publication.
        _server.GivenError(LastBeforePath, HttpStatusCode.NotFound, "error-404.txt");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpNotFoundError>().Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(404);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Returns200WithEmptyArray_ReturnsNotFoundError()
    {
        // Arrange
        _server.GivenJson(LastBeforePath, "[]");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>().Metadata.ContainsKey(HttpStatusExtensions.StatusCodeMetadataKey).ShouldBeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetLastPublishedBeforeAsync_Returns401Or403_ReturnsUnauthorizedError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenError(LastBeforePath, statusCode);

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpUnauthorizedError>().Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Returns429WithRetryAfter_ReturnsRateLimitExceededErrorWithDelay()
    {
        // Arrange
        _server.GivenRetryAfter(LastBeforePath, "30");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpRateLimitExceededError>();
        error.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(429);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetLastPublishedBeforeAsync_Returns5xx_ReturnsServiceUnavailableError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenError(LastBeforePath, statusCode);

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpServiceUnavailableError>().Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Returns200WithMalformedJson_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson(LastBeforePath, "[{\"data\":\"2026-09-30\",\"cena\":");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Returns200WithNullItem_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson(LastBeforePath, "[{\"data\":\"2026-09-30\",\"cena\":512.85},null]");

        // Act
        var result = await CreateSut().GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_Timeout_ReturnsRequestTimeoutError()
    {
        // Arrange
        _server.GivenSlowResponse(LastBeforePath);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await CreateSut(timeout: TimeSpan.FromSeconds(0.1)).GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(NbpWireMockServerExtensions.GaveUpWithin);
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(TimeSpan.FromSeconds(0.1));
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_ConnectionFails_ReturnsConnectionFailedError()
    {
        // Arrange
        var sut = CreateSut();
        _server.Stop();

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(LastBeforeDate, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ConnectionFailedError>().Exception.ShouldBeOfType<HttpRequestException>();
    }
}
