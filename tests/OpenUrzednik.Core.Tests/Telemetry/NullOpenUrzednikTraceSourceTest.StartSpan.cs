using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class NullOpenUrzednikTraceSourceTest
{
    [Fact]
    public void StartSpan_AlwaysReturnSameSpanInstance()
    {
        // Act
        using var instance1 = NullOpenUrzednikTraceSource.Instance.StartSpan("");
        using var instance2 = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Assert
        ReferenceEquals(instance1, instance2).ShouldBeTrue();
    }

    [Fact]
    public void StartSpan_NullSpanName_DoesNotThrowException()
    {
        // Act & Assert
        Should.NotThrow(() => NullOpenUrzednikTraceSource.Instance.StartSpan(""));
    }
}
