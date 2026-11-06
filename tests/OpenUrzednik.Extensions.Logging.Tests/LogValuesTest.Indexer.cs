using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

public sealed partial class LogValuesTest
{
    [Fact]
    public void Indexer_LastIndex_ReturnsOriginalFormat()
    {
        // Arrange
        var sut = Create("value {a}", ("a", 1));

        // Act
        var last = sut[sut.Count - 1];

        // Assert
        sut.Count.ShouldBe(2);
        sut[0].ShouldBe(new("a", 1));
        last.ShouldBe(new("{OriginalFormat}", "value {a}"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException(int index)
    {
        // Arrange
        var sut = Create("value {a}", ("a", 1));

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => sut[index]);
    }
}
