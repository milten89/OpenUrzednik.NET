using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

public sealed partial class ActivitySpanTest
{
    [Fact]
    public void AddEvent_Name_AddsEventWithoutTags()
    {
        // Arrange
        var span = StartSpan();

        // Act
        span.AddEvent("retry");

        // Assert
        var activityEvent = Stop(span).Events.ShouldHaveSingleItem();
        activityEvent.Name.ShouldBe("retry");
        activityEvent.Tags.ShouldBeEmpty();
    }

    [Fact]
    public void AddEvent_NameAndTag_AddsEventWithTag()
    {
        // Arrange
        var span = StartSpan();

        // Act
        span.AddEvent("error", "error.type", "validation");

        // Assert
        var activityEvent = Stop(span).Events.ShouldHaveSingleItem();
        activityEvent.Name.ShouldBe("error");
        activityEvent.Tags.ShouldBe([new("error.type", "validation")]);
    }
}
