using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Transport.Http.Tests;

// The seven HTTP subclasses share one shape, so one class covers them, like Core's StatusCodeMetadataTest (ADR-0021).
public partial class HttpErrorTest
{
    public static TheoryData<OpenUrzednikError, HttpStatusCode> ErrorsWithStatusCode => new()
    {
        { new HttpBadRequestError("message", HttpStatusCode.BadRequest), HttpStatusCode.BadRequest },
        { new HttpUnauthorizedError("message", HttpStatusCode.Forbidden), HttpStatusCode.Forbidden },
        { new HttpNotFoundError("message", HttpStatusCode.NotFound), HttpStatusCode.NotFound },
        { new HttpRequestTimeoutError("message", HttpStatusCode.RequestTimeout), HttpStatusCode.RequestTimeout },
        { new HttpRateLimitExceededError("message", null, (HttpStatusCode)429), (HttpStatusCode)429 },
        { new HttpServiceUnavailableError("message", HttpStatusCode.ServiceUnavailable), HttpStatusCode.ServiceUnavailable },
        { new HttpUnknownError("message", HttpStatusCode.Conflict), HttpStatusCode.Conflict },
    };

    public static TheoryData<OpenUrzednikError, Type, string> ErrorsWithCondition => new()
    {
        { new HttpBadRequestError("message", HttpStatusCode.BadRequest), typeof(BadRequestError), BadRequestError.ErrorCode },
        { new HttpUnauthorizedError("message", HttpStatusCode.Unauthorized), typeof(UnauthorizedError), UnauthorizedError.ErrorCode },
        { new HttpNotFoundError("message", HttpStatusCode.NotFound), typeof(NotFoundError), NotFoundError.ErrorCode },
        { new HttpRequestTimeoutError("message", HttpStatusCode.RequestTimeout), typeof(RequestTimeoutError), RequestTimeoutError.ErrorCode },
        { new HttpRateLimitExceededError("message", null, (HttpStatusCode)429), typeof(RateLimitExceededError), RateLimitExceededError.ErrorCode },
        { new HttpServiceUnavailableError("message", HttpStatusCode.BadGateway), typeof(ServiceUnavailableError), ServiceUnavailableError.ErrorCode },
        { new HttpUnknownError("message", (HttpStatusCode)418), typeof(UnknownError), UnknownError.ErrorCode },
    };

    public static TheoryData<OpenUrzednikError, Type> ErrorsWithException => new()
    {
        { new HttpBadRequestError("message", HttpStatusCode.BadRequest), typeof(BadRequestException) },
        { new HttpUnauthorizedError("message", HttpStatusCode.Unauthorized), typeof(UnauthorizedException) },
        { new HttpNotFoundError("message", HttpStatusCode.NotFound), typeof(NotFoundException) },
        { new HttpRequestTimeoutError("message", HttpStatusCode.RequestTimeout), typeof(RequestTimeoutException) },
        { new HttpRateLimitExceededError("message", null, (HttpStatusCode)429), typeof(RateLimitExceededException) },
        { new HttpServiceUnavailableError("message", HttpStatusCode.BadGateway), typeof(ServiceUnavailableException) },
        { new HttpUnknownError("message", (HttpStatusCode)418), typeof(UnknownException) },
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
    public void Ctor_Always_ImplementsStatusCodeInterface(OpenUrzednikError error, HttpStatusCode statusCode)
    {
        // Assert
        error.ShouldBeAssignableTo<IOpenUrzednikHttpStatusCode>().ShouldNotBeNull().StatusCode.ShouldBe(statusCode);
        error.GetHttpStatus().ShouldBe(statusCode);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithStatusCode))]
    public void Metadata_Always_HoldsTheStatusCodeAsAnInt(OpenUrzednikError error, HttpStatusCode statusCode)
    {
        // Act
        var value = error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey];

        // Assert
        value.ShouldBeOfType<int>().ShouldBe((int)statusCode);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithStatusCode))]
    public void Metadata_Always_FollowsTheMetadataRule(OpenUrzednikError error, HttpStatusCode statusCode)
    {
        // Assert
        _ = statusCode;
        error.ShouldFollowTheMetadataRule();
    }

    [Theory]
    [MemberData(nameof(ErrorsWithStatusCode))]
    public void Metadata_Always_IsReadByCoresObsoleteAccessors(OpenUrzednikError error, HttpStatusCode statusCode)
    {
        // Assert: code written against 1.0 keeps reading the status (ADR-0021)
#pragma warning disable CS0618
        error.GetHttpStatusCode().ShouldBe((int)statusCode);
#pragma warning restore CS0618
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
    public void Metadata_RequestTimeout_KeepsTheConditionsTimeoutKey()
    {
        // Act
        var metadata = new HttpRequestTimeoutError("message", HttpStatusCode.RequestTimeout).Metadata;

        // Assert
        metadata.TryGetValue(RequestTimeoutError.TimeoutMetadataKey, out var timeout).ShouldBeTrue();
        timeout.ShouldBeNull();
        metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(408);
    }

    [Fact]
    public void Ctor_RateLimitWithRetryAfter_KeepsIt()
    {
        // Act
        var error = new HttpRateLimitExceededError("message", TimeSpan.FromSeconds(30), (HttpStatusCode)429);

        // Assert
        error.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        error.Metadata[RateLimitExceededError.RetryAfterDurationMetadataKey].ShouldBe("PT30S");
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(429);
    }
}
