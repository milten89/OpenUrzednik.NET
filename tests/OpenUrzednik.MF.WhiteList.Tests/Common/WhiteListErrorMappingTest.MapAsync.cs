using System.Net;
using System.Net.Http.Headers;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed partial class WhiteListErrorMappingTest
{
    // The 429 body is built from the ministry's error-code table: no blocked response has been captured yet (ADR-0023).
    [Fact]
    public async Task MapAsync_TooManyRequestsWithoutRetryAfter_ReturnsRateLimitUntilMidnightInWarsaw()
    {
        // Act
        var error = await SendAsync(Response((HttpStatusCode)429, Body("WL-191", "Limit zapytań został wyczerpany.")));

        // Assert
        var rateLimit = error.ShouldBeOfType<WhiteListRateLimitExceededError>();
        rateLimit.RetryAfter.ShouldBe(TimeSpan.FromHours(1));
        rateLimit.WhiteListCode.ShouldBe("WL-191");
        rateLimit.StatusCode.ShouldBe((HttpStatusCode)429);
        rateLimit.Message.ShouldBe($"MF White List API rate limit hit on {RequestPath}: WL-191 Limit zapytań został wyczerpany.");
    }

    [Fact]
    public async Task MapAsync_TooManyRequestsWithRetryAfter_ReturnsServersDelay()
    {
        // Arrange
        var response = Response((HttpStatusCode)429, null);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(120));

        // Act
        var error = await SendAsync(response);

        // Assert
        var rateLimit = error.ShouldBeOfType<WhiteListRateLimitExceededError>();
        rateLimit.RetryAfter.ShouldBe(TimeSpan.FromMinutes(2));
        rateLimit.WhiteListCode.ShouldBeNull();
        rateLimit.Message.ShouldBe($"MF White List API rate limit hit on {RequestPath}.");
    }

    [Theory]
    [InlineData("WL-195", "Baza danych została zaktualizowana. Ponów zapytanie.")]
    [InlineData("WL-196", "Trwa aktualizacja danych. Spróbuj później.")]
    public async Task MapAsync_BadRequestAskingToRepeat_ReturnsServiceUnavailableWithBadRequestStatus(string code, string message)
    {
        // Act
        var error = await SendAsync(Response(HttpStatusCode.BadRequest, Body(code, message)));

        // Assert
        var unavailable = error.ShouldBeOfType<WhiteListServiceUnavailableError>();
        unavailable.Code.ShouldBe(ServiceUnavailableError.ErrorCode);
        unavailable.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        unavailable.WhiteListCode.ShouldBe(code);
        unavailable.Message.ShouldBe($"MF White List API asked to repeat the request to {RequestPath} later: {code} {message}");
    }

    [Theory]
    [InlineData("WL-101")]
    [InlineData("WL-115")]
    [InlineData("WL-118")]
    [InlineData("WL-130")]
    [InlineData("WL-190")]
    public async Task MapAsync_BadRequestWithAnotherCode_ReturnsBadRequestWithTheCode(string code)
    {
        // Act
        var error = await SendAsync(Response(HttpStatusCode.BadRequest, Body(code, "Nieprawidłowy parametr.")));

        // Assert
        var badRequest = error.ShouldBeOfType<WhiteListBadRequestError>();
        badRequest.Code.ShouldBe(BadRequestError.ErrorCode);
        badRequest.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        badRequest.WhiteListCode.ShouldBe(code);
        badRequest.Message.ShouldBe($"MF White List API rejected the request to {RequestPath}: {code} Nieprawidłowy parametr.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Bad Request")]
    [InlineData("<html><body>Bad Request</body></html>")]
    [InlineData("""{"message":"Nieprawidłowy NIP.","code":""")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task MapAsync_BadRequestWithoutCodeOrMessage_ReturnsBadRequestWithoutCode(string? body)
    {
        // Act
        var error = await SendAsync(Response(HttpStatusCode.BadRequest, body));

        // Assert
        var badRequest = error.ShouldBeOfType<WhiteListBadRequestError>();
        badRequest.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        badRequest.WhiteListCode.ShouldBeNull();
        badRequest.Message.ShouldBe($"MF White List API rejected the request to {RequestPath}.");
    }

    [Fact]
    public async Task MapAsync_BadRequestWithCodeOnly_PutsTheCodeInTheMessage()
    {
        // Act
        var error = await SendAsync(Response(HttpStatusCode.BadRequest, """{"code":"WL-190"}"""));

        // Assert
        var badRequest = error.ShouldBeOfType<WhiteListBadRequestError>();
        badRequest.WhiteListCode.ShouldBe("WL-190");
        badRequest.Message.ShouldBe($"MF White List API rejected the request to {RequestPath}: WL-190");
    }

    [Fact]
    public async Task MapAsync_BadRequestWithMessageOnly_PutsTheMessageInTheMessage()
    {
        // Act
        var error = await SendAsync(Response(HttpStatusCode.BadRequest, """{"message":"Nieprawidłowy parametr."}"""));

        // Assert
        var badRequest = error.ShouldBeOfType<WhiteListBadRequestError>();
        badRequest.WhiteListCode.ShouldBeNull();
        badRequest.Message.ShouldBe($"MF White List API rejected the request to {RequestPath}: Nieprawidłowy parametr.");
    }

    [Fact]
    public async Task MapAsync_MessageEchoesIdentifiers_MasksThem()
    {
        // Arrange
        var body = Body("WL-115", "Nieprawidłowy NIP 1234567890 lub REGON 123456789 dla konta 12345678901234567890123456.");

        // Act
        var error = await SendAsync(Response(HttpStatusCode.BadRequest, body));

        // Assert
        error.Message.ShouldBe($"MF White List API rejected the request to {RequestPath}: WL-115 "
            + "Nieprawidłowy NIP ******7890 lub REGON *****6789 dla konta **********************3456.");
        error.Message.ShouldNotContain("1234567890");
    }

    [Fact]
    public async Task MapAsync_InternalServerErrorWithHistoricalStatusCode_ReturnsUnknown()
    {
        // Act
        var error = await SendAsync(Response(HttpStatusCode.InternalServerError, Body("WL-197", "Nie można ustalić statusu historycznego.")));

        // Assert
        var unknown = error.ShouldBeOfType<WhiteListUnknownError>();
        unknown.Code.ShouldBe(UnknownError.ErrorCode);
        unknown.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        unknown.WhiteListCode.ShouldBe("WL-197");
        unknown.Message.ShouldBe($"MF White List API can't determine the historical status for {RequestPath}: WL-197 Nie można ustalić statusu historycznego.");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "WL-100")]
    [InlineData(HttpStatusCode.BadGateway, "WL-197")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "WL-100")]
    public async Task MapAsync_ServerErrorWithAnotherCode_ReturnsServiceUnavailable(HttpStatusCode statusCode, string code)
    {
        // Act
        var error = await SendAsync(Response(statusCode, Body(code, "Błąd wewnętrzny.")));

        // Assert
        var unavailable = error.ShouldBeOfType<WhiteListServiceUnavailableError>();
        unavailable.StatusCode.ShouldBe(statusCode);
        unavailable.WhiteListCode.ShouldBe(code);
        unavailable.Message.ShouldBe($"MF White List API is unavailable for {RequestPath}, status {(int)statusCode}: {code} Błąd wewnętrzny.");
    }

    [Fact]
    public async Task MapAsync_ServerErrorWithoutBody_ReturnsServiceUnavailableWithoutCode()
    {
        // Act
        var error = await SendAsync(Response(HttpStatusCode.GatewayTimeout, null));

        // Assert
        var unavailable = error.ShouldBeOfType<WhiteListServiceUnavailableError>();
        unavailable.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        unavailable.WhiteListCode.ShouldBeNull();
        unavailable.Message.ShouldBe($"MF White List API is unavailable for {RequestPath}, status 504.");
    }

    public static TheoryData<HttpStatusCode, Type> UndocumentedStatuses => new()
    {
        { HttpStatusCode.Unauthorized, typeof(HttpUnauthorizedError) },
        { HttpStatusCode.Forbidden, typeof(HttpUnauthorizedError) },
        { HttpStatusCode.NotFound, typeof(HttpNotFoundError) },
        { HttpStatusCode.RequestTimeout, typeof(HttpRequestTimeoutError) },
        { (HttpStatusCode)418, typeof(HttpUnknownError) },
        { (HttpStatusCode)499, typeof(HttpUnknownError) },
        { (HttpStatusCode)302, typeof(HttpUnknownError) },
    };

    [Theory]
    [MemberData(nameof(UndocumentedStatuses))]
    public async Task MapAsync_UndocumentedStatus_UsesTheSharedDefaultMapping(HttpStatusCode statusCode, Type expected)
    {
        // Act
        var error = await SendAsync(Response(statusCode, Body("WL-100", "Błąd.")));

        // Assert
        error.GetType().ShouldBe(expected);
        error.ShouldNotBeAssignableTo<IWhiteListErrorCode>();
        error.GetHttpStatus().ShouldBe(statusCode);
    }

    [Fact]
    public async Task MapAsync_CallerSynchronizationContext_DoesNotResumeOnIt()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        // The body read completes asynchronously and away from the caller's context, so the mapping's await really suspends.
        static async Task<string?> ReadMessageAsync()
        {
            await Task.Delay(1).ConfigureAwait(false);
            return """{"message":"Nieprawidłowy NIP.","code":"WL-115"}""";
        }
        var context = new ErrorResponseContext(response, RequestPath, null, ReadMessageAsync, TestContext.Current.CancellationToken);
        Task<OpenUrzednikError?>? mapping = null;

        // Act
        var posts = await CountingSynchronizationContext.CountPostsAsync(() => mapping = WhiteListErrorMapping.MapAsync(context, new FakeTimeProvider()));

        // Assert
        posts.ShouldBe(0);
        (await mapping!).ShouldBeOfType<WhiteListBadRequestError>().WhiteListCode.ShouldBe("WL-115");
    }
}
