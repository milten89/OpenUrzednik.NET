using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Errors;

// The six error types that carry the API's code share one shape, so one class covers them, like Transport.Http's HttpErrorTest
// (ADR-0021, ADR-0023). WhiteListNotFoundError, which carries the proof instead, is in WhiteListErrorTest.NotFound.cs.
public partial class WhiteListErrorTest
{
    private const string WhiteListCode = "WL-115";

    public static TheoryData<OpenUrzednikError, Type, string> ErrorsWithCode => new()
    {
        { new WhiteListRateLimitExceededError("message", TimeSpan.FromSeconds(30), (HttpStatusCode)429, WhiteListCode), typeof(HttpRateLimitExceededError), RateLimitExceededError.ErrorCode },
        { new WhiteListServiceUnavailableError("message", HttpStatusCode.BadRequest, WhiteListCode), typeof(HttpServiceUnavailableError), ServiceUnavailableError.ErrorCode },
        { new WhiteListBadRequestError("message", HttpStatusCode.BadRequest, WhiteListCode), typeof(HttpBadRequestError), BadRequestError.ErrorCode },
        { new WhiteListUnknownError("message", HttpStatusCode.InternalServerError, WhiteListCode), typeof(HttpUnknownError), UnknownError.ErrorCode },
        { new WhiteListQueryRejectedError("message", WhiteListCode), typeof(BadRequestError), BadRequestError.ErrorCode },
        { new WhiteListServiceInactiveError("message", WhiteListCode), typeof(ServiceUnavailableError), ServiceUnavailableError.ErrorCode },
    };

    public static TheoryData<OpenUrzednikError, Type> ErrorsWithException => new()
    {
        { new WhiteListRateLimitExceededError("message", TimeSpan.FromSeconds(30), (HttpStatusCode)429, WhiteListCode), typeof(RateLimitExceededException) },
        { new WhiteListServiceUnavailableError("message", HttpStatusCode.BadRequest, WhiteListCode), typeof(ServiceUnavailableException) },
        { new WhiteListBadRequestError("message", HttpStatusCode.BadRequest, WhiteListCode), typeof(BadRequestException) },
        { new WhiteListUnknownError("message", HttpStatusCode.InternalServerError, WhiteListCode), typeof(UnknownException) },
        { new WhiteListQueryRejectedError("message", WhiteListCode), typeof(BadRequestException) },
        { new WhiteListServiceInactiveError("message", WhiteListCode), typeof(ServiceUnavailableException) },
    };

    public static TheoryData<OpenUrzednikError> ErrorsWithCodeOnly => new()
    {
        new WhiteListRateLimitExceededError("message", null, (HttpStatusCode)429, WhiteListCode),
        new WhiteListServiceUnavailableError("message", HttpStatusCode.ServiceUnavailable, WhiteListCode),
        new WhiteListBadRequestError("message", HttpStatusCode.BadRequest, WhiteListCode),
        new WhiteListUnknownError("message", HttpStatusCode.InternalServerError, WhiteListCode),
        new WhiteListQueryRejectedError("message", WhiteListCode),
        new WhiteListServiceInactiveError("message", WhiteListCode),
    };

    public static TheoryData<OpenUrzednikError, HttpStatusCode> ErrorsWithStatusCode => new()
    {
        { new WhiteListRateLimitExceededError("message", null, (HttpStatusCode)429, WhiteListCode), (HttpStatusCode)429 },
        { new WhiteListServiceUnavailableError("message", HttpStatusCode.BadRequest, WhiteListCode), HttpStatusCode.BadRequest },
        { new WhiteListBadRequestError("message", HttpStatusCode.BadRequest, WhiteListCode), HttpStatusCode.BadRequest },
        { new WhiteListUnknownError("message", HttpStatusCode.InternalServerError, WhiteListCode), HttpStatusCode.InternalServerError },
    };

    public static TheoryData<OpenUrzednikError> ErrorsWithoutCode => new()
    {
        new WhiteListRateLimitExceededError("message", null, (HttpStatusCode)429, null),
        new WhiteListServiceUnavailableError("message", HttpStatusCode.ServiceUnavailable, null),
        new WhiteListBadRequestError("message", HttpStatusCode.BadRequest, null),
        new WhiteListUnknownError("message", HttpStatusCode.InternalServerError, null),
        new WhiteListQueryRejectedError("message", null),
        new WhiteListServiceInactiveError("message", null),
    };

    [Theory]
    [MemberData(nameof(ErrorsWithCode))]
    public void Ctor_Always_IsItsBaseTypeWithTheConditionsCodeAndTheApisCode(OpenUrzednikError error, Type baseType, string code)
    {
        // Assert
        error.GetType().IsSealed.ShouldBeTrue();
        baseType.IsInstanceOfType(error).ShouldBeTrue();
        error.Code.ShouldBe(code);
        error.Message.ShouldBe("message");
        error.ShouldBeAssignableTo<IWhiteListErrorCode>().ShouldNotBeNull().WhiteListCode.ShouldBe(WhiteListCode);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithStatusCode))]
    public void Ctor_HttpError_KeepsTheStatusCode(OpenUrzednikError error, HttpStatusCode statusCode)
    {
        // Assert
        error.ShouldBeAssignableTo<IOpenUrzednikHttpStatusCode>().ShouldNotBeNull().StatusCode.ShouldBe(statusCode);
        error.GetHttpStatus().ShouldBe(statusCode);
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithCodeOnly))]
    public void Metadata_Always_HoldsTheApisCodeAndFollowsTheMetadataRule(OpenUrzednikError error)
    {
        // Act
        var metadata = error.Metadata;

        // Assert
        metadata[WhiteListErrorMetadata.WhiteListCodeMetadataKey].ShouldBe(WhiteListCode);
        error.ShouldFollowTheMetadataRule();
    }

    [Theory]
    [MemberData(nameof(ErrorsWithoutCode))]
    public void Metadata_NoCode_HoldsNullUnderTheKey(OpenUrzednikError error)
    {
        // Act
        var metadata = error.Metadata;

        // Assert
        error.ShouldBeAssignableTo<IWhiteListErrorCode>().ShouldNotBeNull().WhiteListCode.ShouldBeNull();
        metadata.TryGetValue(WhiteListErrorMetadata.WhiteListCodeMetadataKey, out var value).ShouldBeTrue();
        value.ShouldBeNull();
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Metadata_RateLimit_KeepsTheConditionsKeys()
    {
        // Act
        var metadata = new WhiteListRateLimitExceededError("message", TimeSpan.FromMinutes(90), (HttpStatusCode)429, "WL-191").Metadata;

        // Assert
        metadata[RateLimitExceededError.RetryAfterMetadataKey].ShouldBe(TimeSpan.FromMinutes(90));
        metadata[RateLimitExceededError.RetryAfterDurationMetadataKey].ShouldBe("PT1H30M");
        metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(429);
        metadata[WhiteListErrorMetadata.WhiteListCodeMetadataKey].ShouldBe("WL-191");
    }

    [Fact]
    public void Metadata_Keys_NeverReuseACoreOrTransportKey()
    {
        // Arrange
        string[] reserved =
        [
            HttpStatusExtensions.StatusCodeMetadataKey,
            RateLimitExceededError.RetryAfterMetadataKey,
            RateLimitExceededError.RetryAfterDurationMetadataKey,
            RequestTimeoutError.TimeoutMetadataKey,
            ValidationError.RuleNameMetadataKey,
            ValidationError.NameMetadataKey,
            ValidationError.ValueMetadataKey,
            RangeValidationError.StartNameMetadataKey,
            RangeValidationError.StartMetadataKey,
            RangeValidationError.EndNameMetadataKey,
            RangeValidationError.EndMetadataKey,
        ];

        // Act & Assert
        reserved.ShouldNotContain(WhiteListErrorMetadata.WhiteListCodeMetadataKey);
        reserved.ShouldNotContain(WhiteListNotFoundError.RequestIdMetadataKey);
        reserved.ShouldNotContain(WhiteListNotFoundError.RequestDateTimeMetadataKey);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithException))]
    public void ToException_Always_ReturnsTheConditionsExceptionWithTheError(OpenUrzednikError error, Type exceptionType)
    {
        // Act
        var exception = error.ToException();

        // Assert
        exception.GetType().ShouldBe(exceptionType);
        exception.Message.ShouldBe("message");
        exception.Error.ShouldBeSameAs(error);
    }
}
