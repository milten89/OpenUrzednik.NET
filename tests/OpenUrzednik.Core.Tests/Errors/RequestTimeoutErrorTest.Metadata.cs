using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public partial class RequestTimeoutErrorTest
{
    [Fact]
    public void Metadata_WithTimeout_HasItAsAnIsoDuration()
    {
        // Act
        var error = new RequestTimeoutError("message", TimeSpan.FromSeconds(100));

        // Assert
        error.Metadata[RequestTimeoutError.TimeoutMetadataKey].ShouldBe("PT1M40S");
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Metadata_WithoutTimeout_HasTheKeyWithNull()
    {
        // Act
        var error = new RequestTimeoutError("message");

        // Assert
        error.Metadata.TryGetValue(RequestTimeoutError.TimeoutMetadataKey, out var timeout).ShouldBeTrue();
        timeout.ShouldBeNull();
    }

    [Fact]
    public void TimeoutMetadataKey_Always_HasTheDocumentedValue()
    {
        // Assert
        RequestTimeoutError.TimeoutMetadataKey.ShouldBe("timeout");
    }
}
