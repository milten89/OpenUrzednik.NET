using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public class NullOpenUrzednikMetricsTest
{
    [Fact]
    public void Instance_AlwaysReturnsSameInstance()
    {
        // Act
        var instance1 = NullOpenUrzednikMetrics.Instance;
        var instance2 = NullOpenUrzednikMetrics.Instance;

        // Assert
        ReferenceEquals(instance1, instance2).ShouldBeTrue();
    }

    [Fact]
    public void IsEnabled_AnyName_ReturnsFalse()
    {
        // Act
        var enabled = NullOpenUrzednikMetrics.Instance.IsEnabled("openurzednik.client.operation.duration");

        // Assert
        enabled.ShouldBeFalse();
    }

    [Fact]
    public void Record_AnyMeasurement_DoesNothing()
    {
        // Arrange
        var measurement = new TagsThatThrow();

        // Act & Assert: it never reads the measurement
        Should.NotThrow(() => NullOpenUrzednikMetrics.Instance.Record(measurement));
    }

    private sealed class TagsThatThrow : IOpenUrzednikMeasurement
    {
        public string Name => throw new InvalidOperationException();

        public string Unit => throw new InvalidOperationException();

        public string Description => throw new InvalidOperationException();

        public IReadOnlyList<KeyValuePair<string, object?>> Tags => throw new InvalidOperationException();
    }
}
