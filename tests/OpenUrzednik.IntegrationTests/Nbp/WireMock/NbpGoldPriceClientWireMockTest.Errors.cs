using System.Net;

using OpenUrzednik.Core.Errors;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// Status mapping lives in the shared request executor, so it is tested once, through the gold price client.
public partial class NbpGoldPriceClientWireMockTest
{
    private const string PlainTextUtf8 = "text/plain; charset=utf-8";

    private void GivenErrorResponse(HttpStatusCode statusCode, string? body = null)
    {
        var response = Response.Create().WithStatusCode(statusCode);
        if (body is not null)
            response = response.WithHeader("Content-Type", PlainTextUtf8).WithBody(body);

        _server.Given(Request.Create().WithPath(BasePath).UsingGet()).RespondWith(response);
    }

    [Theory]
    [InlineData("error-400-top-count.txt", "400 BadRequest - Przekroczony limit 255 wyników / Maximum size of 255 data series has been exceeded")]
    [InlineData("error-400-date-range.txt", "400 BadRequest - Przekroczony limit 367 dni / Limit of 367 days has been exceeded")]
    public async Task GetLatestAsync_Returns400_ReturnsBadRequestErrorWithServerMessage(string fixture, string expectedServerMessage)
    {
        // Arrange
        GivenErrorResponse(HttpStatusCode.BadRequest, NbpFixtures.Load(fixture));

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith($": {expectedServerMessage}");
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task GetLatestAsync_Returns404_ReturnsNotFoundError()
    {
        // Arrange
        GivenErrorResponse(HttpStatusCode.NotFound, NbpFixtures.Load("error-404.txt"));

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(404);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetLatestAsync_Returns401Or403_ReturnsUnauthorizedError(HttpStatusCode statusCode)
    {
        // Arrange
        GivenErrorResponse(statusCode);

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
        _server.Given(Request.Create().WithPath(BasePath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.TooManyRequests)
                .WithHeader("Retry-After", "30"));

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
        GivenErrorResponse(statusCode);

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }
}
