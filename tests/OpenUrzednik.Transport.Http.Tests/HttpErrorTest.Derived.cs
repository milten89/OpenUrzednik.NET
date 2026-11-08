using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Transport.Http.Tests;

// A provider derives its own error types from the HTTP subclasses to add typed details of its API (ADR-0022). The types below stand in
// for such provider types: each adds one detail and one key, and keeps the condition's and the transport's details.
public partial class HttpErrorTest
{
    private const string ProviderCode = "XX-195";
    private const string ProviderCodeMetadataKey = "providerCode";

    public static TheoryData<OpenUrzednikError, Type, string, HttpStatusCode> DerivedErrors => new()
    {
        { new ProviderBadRequestError(HttpStatusCode.BadRequest), typeof(HttpBadRequestError), BadRequestError.ErrorCode, HttpStatusCode.BadRequest },
        { new ProviderUnauthorizedError(HttpStatusCode.Forbidden), typeof(HttpUnauthorizedError), UnauthorizedError.ErrorCode, HttpStatusCode.Forbidden },
        { new ProviderNotFoundError(HttpStatusCode.NotFound), typeof(HttpNotFoundError), NotFoundError.ErrorCode, HttpStatusCode.NotFound },
        { new ProviderRequestTimeoutError(HttpStatusCode.RequestTimeout), typeof(HttpRequestTimeoutError), RequestTimeoutError.ErrorCode, HttpStatusCode.RequestTimeout },
        { new ProviderRateLimitExceededError((HttpStatusCode)429), typeof(HttpRateLimitExceededError), RateLimitExceededError.ErrorCode, (HttpStatusCode)429 },
        { new ProviderServiceUnavailableError(HttpStatusCode.BadGateway), typeof(HttpServiceUnavailableError), ServiceUnavailableError.ErrorCode, HttpStatusCode.BadGateway },
        { new ProviderUnknownError((HttpStatusCode)418), typeof(HttpUnknownError), UnknownError.ErrorCode, (HttpStatusCode)418 },
    };

    public static TheoryData<OpenUrzednikError, HttpStatusCode> DerivedErrorsWithStatusCode => new()
    {
        { new ProviderBadRequestError(HttpStatusCode.BadRequest), HttpStatusCode.BadRequest },
        { new ProviderUnauthorizedError(HttpStatusCode.Forbidden), HttpStatusCode.Forbidden },
        { new ProviderNotFoundError(HttpStatusCode.NotFound), HttpStatusCode.NotFound },
        { new ProviderRequestTimeoutError(HttpStatusCode.RequestTimeout), HttpStatusCode.RequestTimeout },
        { new ProviderRateLimitExceededError((HttpStatusCode)429), (HttpStatusCode)429 },
        { new ProviderServiceUnavailableError(HttpStatusCode.BadGateway), HttpStatusCode.BadGateway },
        { new ProviderUnknownError((HttpStatusCode)418), (HttpStatusCode)418 },
    };

    public static TheoryData<OpenUrzednikError, Type> DerivedErrorsWithException => new()
    {
        { new ProviderBadRequestError(HttpStatusCode.BadRequest), typeof(BadRequestException) },
        { new ProviderUnauthorizedError(HttpStatusCode.Forbidden), typeof(UnauthorizedException) },
        { new ProviderNotFoundError(HttpStatusCode.NotFound), typeof(NotFoundException) },
        { new ProviderRequestTimeoutError(HttpStatusCode.RequestTimeout), typeof(RequestTimeoutException) },
        { new ProviderRateLimitExceededError((HttpStatusCode)429), typeof(RateLimitExceededException) },
        { new ProviderServiceUnavailableError(HttpStatusCode.BadGateway), typeof(ServiceUnavailableException) },
        { new ProviderUnknownError((HttpStatusCode)418), typeof(UnknownException) },
    };

    private interface IProviderCode
    {
        string ProviderCode { get; }
    }

    [Theory]
    [MemberData(nameof(DerivedErrors))]
    public void Ctor_DerivedType_IsTheHttpSubclassWithItsStatusAndItsOwnDetail(OpenUrzednikError error, Type httpType, string code, HttpStatusCode statusCode)
    {
        // Assert
        error.ShouldBeAssignableTo(httpType);
        error.Code.ShouldBe(code);
        error.ShouldBeAssignableTo<IOpenUrzednikHttpStatusCode>().ShouldNotBeNull().StatusCode.ShouldBe(statusCode);
        error.GetHttpStatus().ShouldBe(statusCode);
        error.ShouldBeAssignableTo<IProviderCode>().ShouldNotBeNull().ProviderCode.ShouldBe(ProviderCode);
    }

    [Theory]
    [MemberData(nameof(DerivedErrorsWithStatusCode))]
    public void Metadata_DerivedType_KeepsTheStatusAndAddsItsOwnKey(OpenUrzednikError error, HttpStatusCode statusCode)
    {
        // Act
        var metadata = error.Metadata;

        // Assert
        metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
        metadata[ProviderCodeMetadataKey].ShouldBe(ProviderCode);
        error.ShouldFollowTheMetadataRule();
    }

    [Theory]
    [MemberData(nameof(DerivedErrorsWithException))]
    public void ToException_DerivedType_ReturnsTheConditionsExceptionWithTheError(OpenUrzednikError error, Type exceptionType)
    {
        // Act
        var exception = error.ToException();

        // Assert
        exception.GetType().ShouldBe(exceptionType);
        exception.ShouldBeAssignableTo<OpenUrzednikException>().ShouldNotBeNull().Error.ShouldBeSameAs(error);
    }

    [Fact]
    public void Metadata_DerivedRateLimit_KeepsTheConditionsKeys()
    {
        // Act
        var metadata = new ProviderRateLimitExceededError((HttpStatusCode)429).Metadata;

        // Assert
        metadata[RateLimitExceededError.RetryAfterMetadataKey].ShouldBe(TimeSpan.FromSeconds(30));
        metadata[RateLimitExceededError.RetryAfterDurationMetadataKey].ShouldBe("PT30S");
    }

    [Fact]
    public void Metadata_DerivedRequestTimeout_KeepsTheConditionsKey()
    {
        // Act
        var metadata = new ProviderRequestTimeoutError(HttpStatusCode.RequestTimeout).Metadata;

        // Assert
        metadata.TryGetValue(RequestTimeoutError.TimeoutMetadataKey, out var timeout).ShouldBeTrue();
        timeout.ShouldBeNull();
    }

    private static void AddProviderCode(IDictionary<string, object?> metadata) => metadata[ProviderCodeMetadataKey] = ProviderCode;

    private sealed class ProviderBadRequestError(HttpStatusCode statusCode) : HttpBadRequestError("message", statusCode), IProviderCode
    {
        public string ProviderCode => HttpErrorTest.ProviderCode;

        protected override void CollectMetadata(IDictionary<string, object?> metadata)
        {
            base.CollectMetadata(metadata);
            AddProviderCode(metadata);
        }
    }

    private sealed class ProviderUnauthorizedError(HttpStatusCode statusCode) : HttpUnauthorizedError("message", statusCode), IProviderCode
    {
        public string ProviderCode => HttpErrorTest.ProviderCode;

        protected override void CollectMetadata(IDictionary<string, object?> metadata)
        {
            base.CollectMetadata(metadata);
            AddProviderCode(metadata);
        }
    }

    private sealed class ProviderNotFoundError(HttpStatusCode statusCode) : HttpNotFoundError("message", statusCode), IProviderCode
    {
        public string ProviderCode => HttpErrorTest.ProviderCode;

        protected override void CollectMetadata(IDictionary<string, object?> metadata)
        {
            base.CollectMetadata(metadata);
            AddProviderCode(metadata);
        }
    }

    private sealed class ProviderRequestTimeoutError(HttpStatusCode statusCode) : HttpRequestTimeoutError("message", statusCode), IProviderCode
    {
        public string ProviderCode => HttpErrorTest.ProviderCode;

        protected override void CollectMetadata(IDictionary<string, object?> metadata)
        {
            base.CollectMetadata(metadata);
            AddProviderCode(metadata);
        }
    }

    private sealed class ProviderRateLimitExceededError(HttpStatusCode statusCode)
        : HttpRateLimitExceededError("message", TimeSpan.FromSeconds(30), statusCode), IProviderCode
    {
        public string ProviderCode => HttpErrorTest.ProviderCode;

        protected override void CollectMetadata(IDictionary<string, object?> metadata)
        {
            base.CollectMetadata(metadata);
            AddProviderCode(metadata);
        }
    }

    private sealed class ProviderServiceUnavailableError(HttpStatusCode statusCode) : HttpServiceUnavailableError("message", statusCode), IProviderCode
    {
        public string ProviderCode => HttpErrorTest.ProviderCode;

        protected override void CollectMetadata(IDictionary<string, object?> metadata)
        {
            base.CollectMetadata(metadata);
            AddProviderCode(metadata);
        }
    }

    private sealed class ProviderUnknownError(HttpStatusCode statusCode) : HttpUnknownError("message", statusCode), IProviderCode
    {
        public string ProviderCode => HttpErrorTest.ProviderCode;

        protected override void CollectMetadata(IDictionary<string, object?> metadata)
        {
            base.CollectMetadata(metadata);
            AddProviderCode(metadata);
        }
    }
}
