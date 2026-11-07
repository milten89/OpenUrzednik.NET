using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class RestRequestExecutorTest
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    [Fact]
    public void LimitElapsed_ElapsedEqualsLimit_ReturnsTrue()
    {
        // Act & Assert
        RestRequestExecutor.LimitElapsed(Limit, Limit).ShouldBeTrue();
    }

    [Fact]
    public void LimitElapsed_ElapsedWithinTimerResolutionBelowLimit_ReturnsTrue()
    {
        // Arrange
        // A timer may fire up to TimerResolution early: exactly that early still counts as the limit.
        var elapsed = Limit - RestRequestExecutor.TimerResolution;

        // Act & Assert
        RestRequestExecutor.LimitElapsed(elapsed, Limit).ShouldBeTrue();
    }

    [Fact]
    public void LimitElapsed_ElapsedJustBeyondTimerResolutionBelowLimit_ReturnsFalse()
    {
        // Arrange
        var elapsed = Limit - RestRequestExecutor.TimerResolution - TimeSpan.FromTicks(1);

        // Act & Assert
        RestRequestExecutor.LimitElapsed(elapsed, Limit).ShouldBeFalse();
    }
}
