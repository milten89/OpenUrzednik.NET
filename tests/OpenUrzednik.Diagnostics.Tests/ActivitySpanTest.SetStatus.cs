using System.Diagnostics;

using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

public sealed partial class ActivitySpanTest
{
    [Theory]
    [InlineData(OpenUrzednikSpanStatus.Unset, ActivityStatusCode.Unset)]
    [InlineData(OpenUrzednikSpanStatus.Ok, ActivityStatusCode.Ok)]
    [InlineData(OpenUrzednikSpanStatus.Error, ActivityStatusCode.Error)]
    public void SetStatus_Status_MapsToActivityStatus(OpenUrzednikSpanStatus status, ActivityStatusCode expected)
    {
        // Arrange
        var span = StartSpan();

        // Act
        span.SetStatus(status);

        // Assert
        Stop(span).Status.ShouldBe(expected);
    }

    [Fact]
    public void SetStatus_ErrorWithDescription_KeepsDescription()
    {
        // Arrange
        var span = StartSpan();

        // Act
        span.SetStatus(OpenUrzednikSpanStatus.Error, "Network failure");

        // Assert
        Stop(span).StatusDescription.ShouldBe("Network failure");
    }
}
