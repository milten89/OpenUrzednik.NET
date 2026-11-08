using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Transport.Http.Tests;

// The seven HTTP subclasses share one shape, so one class covers them, like Core's StatusCodeMetadataTest (ADR-0021).
public class HttpErrorTest
{
    public static TheoryData<OpenUrzednikError, int> ErrorsWithStatusCode => new()
    {
        { new HttpBadRequestError("message", 400), 400 },
        { new HttpUnauthorizedError("message", 403), 403 },
        { new HttpNotFoundError("message", 404), 404 },
        { new HttpRequestTimeoutError("message", 408), 408 },
        { new HttpRateLimitExceededError("message", null, 429), 429 },
        { new HttpServiceUnavailableError("message", 503), 503 },
        { new HttpUnknownError("message", 409), 409 },
    };

    public static TheoryData<OpenUrzednikError, Type, string> ErrorsWithCondition => new()
    {
        { new HttpBadRequestError("message", 400), typeof(BadRequestError), BadRequestError.ErrorCode },
        { new HttpUnauthorizedError("message", 401), typeof(UnauthorizedError), UnauthorizedError.ErrorCode },
        { new HttpNotFoundError("message", 404), typeof(NotFoundError), NotFoundError.ErrorCode },
        { new HttpRequestTimeoutError("message", 408), typeof(RequestTimeoutError), RequestTimeoutError.ErrorCode },
        { new HttpRateLimitExceededError("message", null, 429), typeof(RateLimitExceededError), RateLimitExceededError.ErrorCode },
        { new HttpServiceUnavailableError("message", 502), typeof(ServiceUnavailableError), ServiceUnavailableError.ErrorCode },
        { new HttpUnknownError("message", 418), typeof(UnknownError), UnknownError.ErrorCode },
    };

    public static TheoryData<OpenUrzednikError, Type> ErrorsWithException => new()
    {
        { new HttpBadRequestError("message", 400), typeof(BadRequestException) },
        { new HttpUnauthorizedError("message", 401), typeof(UnauthorizedException) },
        { new HttpNotFoundError("message", 404), typeof(NotFoundException) },
        { new HttpRequestTimeoutError("message", 408), typeof(RequestTimeoutException) },
        { new HttpRateLimitExceededError("message", null, 429), typeof(RateLimitExceededException) },
        { new HttpServiceUnavailableError("message", 502), typeof(ServiceUnavailableException) },
        { new HttpUnknownError("message", 418), typeof(UnknownException) },
    };

    [Theory]
    [MemberData(nameof(ErrorsWithCondition))]
    public void Ctor_Always_IsItsConditionWithTheConditionsCodeAndMessage(OpenUrzednikError error, Type condition, string code)
    {
        // Assert
        condition.IsInstanceOfType(error).ShouldBeTrue();
        error.Code.ShouldBe(code);
        error.Message.ShouldBe("message");
    }

    [Theory]
    [MemberData(nameof(ErrorsWithStatusCode))]
    public void Ctor_Always_ImplementsStatusCodeInterface(OpenUrzednikError error, int statusCode)
    {
        // Assert
        error.ShouldBeAssignableTo<IOpenUrzednikHttpStatusCode>().ShouldNotBeNull().StatusCode.ShouldBe(statusCode);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithStatusCode))]
    public void Ctor_Always_StoresStatusCodeForCoreAccessors(OpenUrzednikError error, int statusCode)
    {
        // Assert
        error.Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe(statusCode);
        error.TryGetHttpStatusCode(out var found).ShouldBeTrue();
        found.ShouldBe(statusCode);
        error.GetHttpStatusCode().ShouldBe(statusCode);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithException))]
    public void ToException_Always_ReturnsTheConditionsExceptionWithTheError(OpenUrzednikError error, Type exceptionType)
    {
        // Act
        var exception = error.ToException();

        // Assert
        exception.GetType().ShouldBe(exceptionType);
        exception.Error.ShouldBeSameAs(error);
        exception.Message.ShouldBe("message");
    }

    [Fact]
    public void Ctor_RateLimitWithRetryAfter_KeepsIt()
    {
        // Act
        var error = new HttpRateLimitExceededError("message", TimeSpan.FromSeconds(30), 429);

        // Assert
        error.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        error.Metadata[RateLimitExceededError.RetryAfterMetadataKey].ShouldBe(TimeSpan.FromSeconds(30));
        error.StatusCode.ShouldBe(429);
    }
}
