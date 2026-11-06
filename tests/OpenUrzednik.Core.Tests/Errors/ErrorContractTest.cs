using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

// The codes and metadata keys are part of the 1.0 error contract (ADR-0002): changing one is a breaking change.
public class ErrorContractTest
{
    [Theory]
    [InlineData(BadRequestError.ErrorCode, "badRequest")]
    [InlineData(ConnectionFailedError.ErrorCode, "connectionFailed")]
    [InlineData(NotFoundError.ErrorCode, "notFound")]
    [InlineData(RateLimitExceededError.ErrorCode, "rateLimitExceeded")]
    [InlineData(RequestTimeoutError.ErrorCode, "requestTimeout")]
    [InlineData(SerializationError.ErrorCode, "serialization")]
    [InlineData(ServiceUnavailableError.ErrorCode, "serviceUnavailable")]
    [InlineData(UnauthorizedError.ErrorCode, "unauthorized")]
    [InlineData(UnknownError.ErrorCode, "unknown")]
    [InlineData(ValidationError.ErrorCode, "validation")]
    public void ErrorCode_Always_IsTheDocumentedValue(string actual, string expected)
    {
        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void RateLimitExceededError_WithRetryAfter_StoresItUnderRetryAfterMetadataKey()
    {
        // Act
        var error = new RateLimitExceededError("message", TimeSpan.FromSeconds(30), 429);

        // Assert
        RateLimitExceededError.RetryAfterMetadataKey.ShouldBe("retryAfter");
        error.Metadata[RateLimitExceededError.RetryAfterMetadataKey].ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ValidationError_WithNameAndValue_StoresThemUnderTheirMetadataKeys()
    {
        // Act
        var error = new ValidationError("message", "range", "topCount", 300);

        // Assert
        ValidationError.RuleNameMetadataKey.ShouldBe("ruleName");
        ValidationError.NameMetadataKey.ShouldBe("name");
        ValidationError.ValueMetadataKey.ShouldBe("value");
        error.Metadata[ValidationError.RuleNameMetadataKey].ShouldBe("range");
        error.Metadata[ValidationError.NameMetadataKey].ShouldBe("topCount");
        error.Metadata[ValidationError.ValueMetadataKey].ShouldBe(300);
    }
}
