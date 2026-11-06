using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class NullOpenUrzednikTraceSourceTest
{
    [Theory]
    [InlineData(OpenUrzednikSpanStatus.Ok)]
    [InlineData(OpenUrzednikSpanStatus.Error)]
    [InlineData(OpenUrzednikSpanStatus.Unset)]
    public void SetStatus_DoesNotThrowException(OpenUrzednikSpanStatus status)
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Act && Assert
        Should.NotThrow(() => span.SetStatus(status));
    }
}
