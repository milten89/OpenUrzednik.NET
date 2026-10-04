using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class NullOpenUrzednikTraceSourceTest
{
    [Fact]
    public void SetTag_NullArgs_DoesNotThrowException()
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Act && Assert
        Should.NotThrow(() => span.SetTag<object>(null!, null!));
    }
}
