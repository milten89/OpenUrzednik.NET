using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public class NullOpenUrzednikTraceSourceTest
{
    [Fact]
    public void Instance_AlwaysReturnSameInstance()
    {
        // Act
        var instance1 = NullOpenUrzednikTraceSource.Instance;
        var instance2 = NullOpenUrzednikTraceSource.Instance;

        // Assert
        ReferenceEquals(instance1, instance2).ShouldBeTrue();
    }

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

    [Fact]
    public void IsRecording_ReturnFalse()
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        //  Assert
        span.IsRecording.ShouldBeFalse();
    }

    [Fact]
    public void SetTag_NullArgs_DoesNotThrowException()
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Asc & Assert
        Should.NotThrow(() => span.SetTag<object>(null!, null!));
    }

    [Theory]
    [InlineData(OpenUrzednikSpanStatus.Ok)]
    [InlineData(OpenUrzednikSpanStatus.Error)]
    [InlineData(OpenUrzednikSpanStatus.Unset)]
    public void SetStatus_DoesNotThrowException(OpenUrzednikSpanStatus status)
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Asc & Assert
        Should.NotThrow(() => span.SetStatus(status));
    }

    [Fact]
    public void RecordException_NullArgs_DoesNotThrowException()
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Asc & Assert
        Should.NotThrow(() => span.RecordException(null!));
    }

    [Fact]
    public void AddEvent_NullArgs_DoesNotThrowException()
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Asc & Assert
        Should.NotThrow(() => span.AddEvent(null!));
    }

    [Fact]
    public void AddEventGeneric_NullArgs_DoesNotThrowException()
    {
        // Arrange
        using var span = NullOpenUrzednikTraceSource.Instance.StartSpan("");

        // Asc & Assert
        Should.NotThrow(() => span.AddEvent<object>(null!, null!, null!));
    }
}
