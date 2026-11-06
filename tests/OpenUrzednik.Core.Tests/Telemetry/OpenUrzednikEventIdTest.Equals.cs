using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class OpenUrzednikEventIdTest
{
    [Fact]
    public void Equals_SameIdAndName_ReturnsTrue()
    {
        // Arrange
        var first = new OpenUrzednikEventId(1001, "RequestTimedOut");
        var second = new OpenUrzednikEventId(1001, "RequestTimedOut");

        // Act & Assert
        (first == second).ShouldBeTrue();
        first.Equals((object)second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Theory]
    [InlineData(1002, "RequestTimedOut")]
    [InlineData(1001, "ConnectionFailed")]
    public void Equals_DifferentIdOrName_ReturnsFalse(int id, string name)
    {
        // Arrange
        var first = new OpenUrzednikEventId(1001, "RequestTimedOut");

        // Act & Assert
        (first != new OpenUrzednikEventId(id, name)).ShouldBeTrue();
    }
}
