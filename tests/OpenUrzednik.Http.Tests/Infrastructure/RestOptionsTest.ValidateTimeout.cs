using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class RestOptionsTest
{
    public static TheoryData<TimeSpan?> ValidTimeouts => new()
    {
        (TimeSpan?)null,
        TimeSpan.FromTicks(1),
        TimeSpan.FromSeconds(5),
        // HttpClient.Timeout accepts at most int.MaxValue milliseconds, and so does the option.
        TimeSpan.FromMilliseconds(int.MaxValue),
        Timeout.InfiniteTimeSpan,
    };

    public static TheoryData<TimeSpan> InvalidTimeouts => new()
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(-5),
        TimeSpan.FromMilliseconds(-2),
        TimeSpan.FromMilliseconds(int.MaxValue) + TimeSpan.FromMilliseconds(1),
    };

    [Theory]
    [MemberData(nameof(ValidTimeouts))]
    public void ValidateTimeout_ValidTimeout_DoesNotThrow(TimeSpan? timeout)
    {
        // Act & Assert
        Should.NotThrow(() => RestOptions.ValidateTimeout(timeout, DisplayName, "timeout"));
    }

    [Theory]
    [MemberData(nameof(InvalidTimeouts))]
    public void ValidateTimeout_InvalidTimeout_ThrowsArgumentException(TimeSpan timeout)
    {
        // Act
        var exception = Should.Throw<ArgumentException>(() => RestOptions.ValidateTimeout(timeout, DisplayName, "timeout"));

        // Assert
        exception.ParamName.ShouldBe("timeout");
        exception.Message.ShouldStartWith($"Invalid Test API timeout: {timeout}. It must be positive and at most {int.MaxValue} ms");
    }
}
