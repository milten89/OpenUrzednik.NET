using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

public sealed partial class ActivitySpanTest
{
    [Fact]
    public void SetTag_Values_KeepsTypes()
    {
        // Arrange
        var span = StartSpan();

        // Act
        span.SetTag("nbp.top_count", 3);
        span.SetTag("nbp.currency", "USD");

        // Assert
        var activity = Stop(span);
        activity.GetTagItem("nbp.top_count").ShouldBe(3);
        activity.GetTagItem("nbp.currency").ShouldBe("USD");
    }
}
