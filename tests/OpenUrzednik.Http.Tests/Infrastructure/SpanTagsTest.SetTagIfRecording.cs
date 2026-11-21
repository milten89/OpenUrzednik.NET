using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class SpanTagsTest
{
    [Fact]
    public void SetTagIfRecording_Recording_SetsTheBuiltValue()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        var calls = 0;

        // Act
        span.SetTagIfRecording("test.key", 42, value =>
        {
            calls++;
            return $"value-{value}";
        });

        // Assert
        span.Received(1).SetTag("test.key", "value-42");
        calls.ShouldBe(1);
    }

    [Fact]
    public void SetTagIfRecording_NotRecording_SetsNothingAndDoesNotBuildTheValue()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(false);
        var calls = 0;

        // Act
        span.SetTagIfRecording("test.key", 42, value =>
        {
            calls++;
            return $"value-{value}";
        });

        // Assert
        span.DidNotReceiveWithAnyArgs().SetTag<string>(default!, default!);
        calls.ShouldBe(0);
    }
}
