using System.Net;

using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListRetryTest
{
    [Theory]
    [InlineData(400, "WL-195")]
    [InlineData(400, "WL-196")]
    [InlineData(500, "WL-115")]
    [InlineData(500, "WL-196")]
    [InlineData(500, "WL-19")]
    public async Task ShouldRetryAsync_RetryableStatusWithCode_ReturnsTrue(int statusCode, string code)
    {
        // Arrange
        using var response = Response(statusCode, Body(code));

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        retry.ShouldBeTrue();
    }

    [Theory]
    [InlineData(400, "WL-115")]
    [InlineData(400, "WL-197")]
    [InlineData(400, "WL-191")]
    [InlineData(400, "WL-1950")]
    [InlineData(400, "wl-195")]
    [InlineData(500, "WL-197")]
    public async Task ShouldRetryAsync_OtherCode_FollowsTheRuleOfItsStatus(int statusCode, string code)
    {
        // Arrange
        using var response = Response(statusCode, Body(code));

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert: a 400 only asks to repeat for WL-195/196, and a 500 is retried for any code but WL-197.
        retry.ShouldBe(statusCode == 500 && code != "WL-197");
    }

    [Theory]
    [InlineData(408)]
    [InlineData(501)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    [InlineData(599)]
    public async Task ShouldRetryAsync_TimeoutOrServerErrorWithoutBody_ReturnsTrue(int statusCode)
    {
        // Arrange
        using var response = Response(statusCode, null);

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        retry.ShouldBeTrue();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(204)]
    [InlineData(301)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(407)]
    [InlineData(409)]
    [InlineData(429)]
    [InlineData(600)]
    public async Task ShouldRetryAsync_OtherStatus_ReturnsFalse(int statusCode)
    {
        // Arrange: the codes that make a 400 or 500 retryable change nothing for another status
        using var response = Response(statusCode, Body("WL-195"));

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        retry.ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldRetryAsync_TooManyRequestsWithDailyLimitCode_ReturnsFalse()
    {
        // Arrange
        using var response = Response(429, Body("WL-191"));

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        retry.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>Bad Gateway</html>")]
    [InlineData("""{"message":"WL-195"}""")]
    [InlineData("""{"code":null}""")]
    [InlineData("[]")]
    [InlineData("""{"code":"1234567890"}""")]
    [InlineData("""{"code":"WL-195" """)]
    public async Task ShouldRetryAsync_BadRequestWithoutTheCodes_ReturnsFalse(string? body)
    {
        // Arrange
        using var response = Response(400, body);

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        retry.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>Internal Server Error</html>")]
    [InlineData("""{"code":"1234567890"}""")]
    public async Task ShouldRetryAsync_InternalServerErrorWithoutTheHistoricalStatusCode_ReturnsTrue(string? body)
    {
        // Arrange
        using var response = Response(500, body);

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        retry.ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldRetryAsync_NoContent_HasNoCode()
    {
        // Arrange: .NET Framework leaves Content null for a response without a body
        using var badRequest = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = null! };
        using var serverError = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = null! };

        // Act
        var badRequestRetry = await WhiteListRetry.ShouldRetryAsync(badRequest, TestContext.Current.CancellationToken);
        var serverErrorRetry = await WhiteListRetry.ShouldRetryAsync(serverError, TestContext.Current.CancellationToken);

        // Assert
        badRequestRetry.ShouldBeFalse();
        serverErrorRetry.ShouldBeTrue();
    }

    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    public async Task ShouldRetryAsync_BodyThatCantBeRead_HasNoCode(int statusCode)
    {
        // Arrange
        using var ioFailure = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = new FailingContent(new IOException("Connection reset.")) };
        using var httpFailure = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = new FailingContent(new HttpRequestException("Connection closed.")) };

        // Act
        var ioRetry = await WhiteListRetry.ShouldRetryAsync(ioFailure, TestContext.Current.CancellationToken);
        var httpRetry = await WhiteListRetry.ShouldRetryAsync(httpFailure, TestContext.Current.CancellationToken);

        // Assert: no code, so a 400 isn't retried and a 500 is
        ioRetry.ShouldBe(statusCode == 500);
        httpRetry.ShouldBe(statusCode == 500);
    }

    [Theory]
    [InlineData(400, "WL-195")]
    [InlineData(500, "WL-197")]
    [InlineData(400, "WL-115")]
    public async Task ShouldRetryAsync_AfterTheCall_TheBodyIsStillReadable(int statusCode, string code)
    {
        // Arrange
        var body = Body(code);
        using var response = OneShotResponse(statusCode, body);

        // Act
        await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert: the executor reads the stream, and the first read of an unbuffered content would be the only one
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        var first = await response.Content.ReadAsStringAsync();
        var second = await response.Content.ReadAsStringAsync();
#pragma warning restore xUnit1051
        first.ShouldBe(body);
        second.ShouldBe(body);
    }

    [Fact]
    public void ShouldRetryAsync_NullResponse_ThrowsSynchronously()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => _ = WhiteListRetry.ShouldRetryAsync(null!));

        // Assert: thrown by the call itself, before any task exists
        exception.ParamName.ShouldBe("response");
    }

    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    public async Task ShouldRetryAsync_CancelledToken_Throws(int statusCode)
    {
        // Arrange
        using var response = Response(statusCode, Body("WL-195"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await WhiteListRetry.ShouldRetryAsync(response, cts.Token));
    }

    [Fact]
    public async Task ShouldRetryAsync_CancelledTokenForAStatusWithoutBodyRead_StillAnswers()
    {
        // Arrange: the token only matters for the body of a 400 or 500
        using var response = Response(503, null);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, cts.Token);

        // Assert
        retry.ShouldBeTrue();
    }

    [Theory]
    [InlineData(400, "WL-195", true)]
    [InlineData(500, "WL-197", false)]
    public async Task ShouldRetryAsync_BodyArrivingAsynchronously_NeverResumesOnTheCallersSynchronizationContext(int statusCode, string code, bool expected)
    {
        // Arrange
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = new AsyncContent(Body(code)) };
        Task<bool>? answer = null;

        // Act
        var posts = await CountingSynchronizationContext.CountPostsAsync(() =>
            answer = WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken).AsTask());
        var retry = await answer!;

        // Assert
        posts.ShouldBe(0);
        retry.ShouldBe(expected);
    }
}
