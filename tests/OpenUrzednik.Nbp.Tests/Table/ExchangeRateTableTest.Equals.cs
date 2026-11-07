using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public sealed partial class ExchangeRateTableTest
{
    [Fact]
    public void Equals_SameValuesInDifferentLists_ReturnsTrue()
    {
        // Arrange
        var sut = Create();
        var other = Create();

        // Act
        var result = sut.Equals(other);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var sut = Create();

        // Act
        var result = sut.Equals(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentTableId_ReturnsFalse()
    {
        // Arrange
        var sut = Create();
        var other = Create() with { TableId = "002/A/NBP/2026" };

        // Act
        var result = sut.Equals(other);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentPublicationDate_ReturnsFalse()
    {
        // Arrange
        var sut = Create();
        var other = Create() with { PublicationDate = new DateTime(2026, 1, 5) };

        // Act
        var result = sut.Equals(other);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentRateInList_ReturnsFalse()
    {
        // Arrange
        var sut = Create();
        var other = Create() with { Rates = [sut.Rates[0], sut.Rates[1] with { Price = 4.3000m }] };

        // Act
        var result = sut.Equals(other);

        // Assert
        result.ShouldBeFalse();
    }
}
