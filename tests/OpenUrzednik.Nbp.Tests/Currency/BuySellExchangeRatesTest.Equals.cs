using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public sealed partial class BuySellExchangeRatesTest
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
    public void Equals_DifferentCurrencyName_ReturnsFalse()
    {
        // Arrange
        var sut = Create();
        var other = Create() with { CurrencyName = "dolar kanadyjski" };

        // Act
        var result = sut.Equals(other);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentCurrencyCode_ReturnsFalse()
    {
        // Arrange
        var sut = Create();
        var other = Create() with { CurrencyCode = "CAD" };

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
        var other = Create() with { Rates = [sut.Rates[0], sut.Rates[1] with { Bid = 3.6200m }] };

        // Act
        var result = sut.Equals(other);

        // Assert
        result.ShouldBeFalse();
    }
}
