using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

public sealed partial class LogValuesTest
{
    [Fact]
    public void GetEnumerator_Values_EnumeratesValuesThenOriginalFormat()
    {
        // Arrange
        var sut = Create("{a} {b}", ("a", 1), ("b", 2));

        // Act
        var items = sut.ToList();

        // Assert
        items.ShouldBe([new("a", 1), new("b", 2), new("{OriginalFormat}", "{a} {b}")]);
    }
}
