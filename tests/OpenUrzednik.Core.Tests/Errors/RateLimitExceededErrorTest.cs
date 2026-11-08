using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public class RateLimitExceededErrorTest
{
    [Fact]
    public void Metadata_WithRetryAfter_HasTheTimeSpanAndTheIsoDuration()
    {
        // Act
        var error = new RateLimitExceededError("message", TimeSpan.FromSeconds(90));

        // Assert
        error.Metadata[RateLimitExceededError.RetryAfterMetadataKey].ShouldBe(TimeSpan.FromSeconds(90));
        error.Metadata[RateLimitExceededError.RetryAfterDurationMetadataKey].ShouldBe("PT1M30S");
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Metadata_WithoutRetryAfter_HasBothKeysWithNull()
    {
        // Act
        var error = new RateLimitExceededError("message", null);

        // Assert
        error.Metadata.TryGetValue(RateLimitExceededError.RetryAfterMetadataKey, out var retryAfter).ShouldBeTrue();
        retryAfter.ShouldBeNull();
        error.Metadata.TryGetValue(RateLimitExceededError.RetryAfterDurationMetadataKey, out var retryAfterDuration).ShouldBeTrue();
        retryAfterDuration.ShouldBeNull();
    }

    [Fact]
    public void RetryAfterDurationMetadataKey_Always_HasTheDocumentedValue()
    {
        // Assert
        RateLimitExceededError.RetryAfterDurationMetadataKey.ShouldBe("retryAfterDuration");
    }
}
