using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public sealed partial class CurrencyExchangeRatesTest
{
    [Fact]
    public void GetHashCode_SameValuesInDifferentLists_ReturnsSameHashCode()
    {
        // Arrange
        var sut = Create();
        var other = Create();

        // Act
        var result = sut.GetHashCode();

        // Assert
        result.ShouldBe(other.GetHashCode());
    }
}
