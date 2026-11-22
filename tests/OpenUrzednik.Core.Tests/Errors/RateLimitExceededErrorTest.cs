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
#pragma warning disable CS0618 // the obsolete 1.0 key is still written until 2.0 (ADR-0021)
        error.Metadata[RateLimitExceededError.RetryAfterMetadataKey].ShouldBe(TimeSpan.FromSeconds(90));
#pragma warning restore CS0618
        error.Metadata[RateLimitExceededError.RetryAfterDurationMetadataKey].ShouldBe("PT1M30S");
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Metadata_WithoutRetryAfter_HasBothKeysWithNull()
    {
        // Act
        var error = new RateLimitExceededError("message", null);

        // Assert
#pragma warning disable CS0618 // the obsolete 1.0 key is still written until 2.0 (ADR-0021)
        error.Metadata.TryGetValue(RateLimitExceededError.RetryAfterMetadataKey, out var retryAfter).ShouldBeTrue();
#pragma warning restore CS0618
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
