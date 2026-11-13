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
        // HttpClient.Timeout accepts at most int.MaxValue milliseconds, and so do the options and the executor.
        TimeSpan.FromMilliseconds(int.MaxValue),
        Timeout.InfiniteTimeSpan,
    };

    public static TheoryData<TimeSpan> InvalidTimeouts => new()
    {
        TimeSpan.Zero,
        TimeSpan.FromTicks(-1),
        TimeSpan.FromSeconds(-5),
        TimeSpan.FromMilliseconds(int.MaxValue) + TimeSpan.FromTicks(1),
    };

    [Theory]
    [MemberData(nameof(ValidTimeouts))]
    public void IsValidTimeout_ValidTimeout_ReturnsTrue(TimeSpan? timeout)
    {
        // Act
        var result = RestOptions.IsValidTimeout(timeout);

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidTimeouts))]
    public void IsValidTimeout_InvalidTimeout_ReturnsFalse(TimeSpan timeout)
    {
        // Act
        var result = RestOptions.IsValidTimeout(timeout);

        // Assert
        result.ShouldBeFalse();
    }
}
