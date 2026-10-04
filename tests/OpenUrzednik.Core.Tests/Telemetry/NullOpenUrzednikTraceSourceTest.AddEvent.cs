using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class NullOpenUrzednikTraceSourceTest
{
    [Fact]
    public void AddEvent_NullArgs_DoesNotThrowException()
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Act && Assert
        Should.NotThrow(() => span.AddEvent(null!));
    }

    [Fact]
    public void AddEventGeneric_NullArgs_DoesNotThrowException()
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Act && Assert
        Should.NotThrow(() => span.AddEvent<object>(null!, null!, null!));
    }
}
