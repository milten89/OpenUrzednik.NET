using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

public sealed partial class ActivitySpanTest
{
    [Fact]
    public void RecordException_Exception_AddsExceptionEventWithSemanticConventionTags()
    {
        // Arrange
        var span = StartSpan();
        var exception = new HttpRequestException("Connection refused");

        // Act
        span.RecordException(exception);

        // Assert
        var activityEvent = Stop(span).Events.ShouldHaveSingleItem();
        activityEvent.Name.ShouldBe("exception");
        var tags = activityEvent.Tags.ToDictionary();
        tags["exception.type"].ShouldBe("System.Net.Http.HttpRequestException");
        tags["exception.message"].ShouldBe("Connection refused");
        tags["exception.stacktrace"].ShouldBe(exception.ToString());
    }

    [Fact]
    public void RecordException_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        using var span = StartSpan();

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => span.RecordException(null!));

        // Assert
        exception.ParamName.ShouldBe("exception");
    }
}
