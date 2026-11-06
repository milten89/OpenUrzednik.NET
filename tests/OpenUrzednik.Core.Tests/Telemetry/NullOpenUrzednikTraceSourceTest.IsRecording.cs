using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class NullOpenUrzednikTraceSourceTest
{
    [Fact]
    public void IsRecording_ReturnFalse()
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        //  Assert
        span.IsRecording.ShouldBeFalse();
    }
}
