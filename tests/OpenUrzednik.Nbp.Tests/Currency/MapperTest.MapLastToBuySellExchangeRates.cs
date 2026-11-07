using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class MapperTest
{
    [Fact]
    public void MapLastToBuySellExchangeRates_UnsortedRates_ReturnsOnlyRateWithLatestPublicationDate()
    {
        // Arrange
        var dto = new BuySellCurrencyExchangeRatesDtoFaker(5).WithConstantSeed().Generate();
        var expected = dto.Rates.OrderByDescending(x => x.PublicationDate).First();

        // Act
        var result = Mapper.MapLastToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName!, dto.CurrencyCode,
            [new CurrencyBuySellRate(expected.TableId, expected.PublicationDate.ToPublicDate(), expected.Ask, expected.Bid)]));
    }

    [Fact]
    public void MapLastToBuySellExchangeRates_EmptyRates_ReturnsNull()
    {
        // Arrange
        var dto = new BuySellCurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapLastToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void MapLastToBuySellExchangeRates_NullDto_ThrowsInvalidPayloadException()
    {
        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToBuySellExchangeRates(null!))
            .Message.ShouldBe("NBP API response is missing 'item'.");
    }

    [Fact]
    public void MapLastToBuySellExchangeRates_NullRates_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dtoBase = new BuySellCurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();
        var dto = new BuySellCurrencyExchangeRatesDto { CurrencyName = dtoBase.CurrencyName, CurrencyCode = dtoBase.CurrencyCode, Rates = null! };

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToBuySellExchangeRates(dto))
            .Message.ShouldBe("NBP API response is missing 'rates'.");
    }

    [Fact]
    public void MapLastToBuySellExchangeRates_RatesContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dtoBase = new BuySellCurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();
        var dto = new BuySellCurrencyExchangeRatesDto { CurrencyName = dtoBase.CurrencyName, CurrencyCode = dtoBase.CurrencyCode, Rates = [dtoBase.Rates[0], null!] };

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToBuySellExchangeRates(dto))
            .Message.ShouldBe("NBP API response is missing 'item'.");
    }

    [Fact]
    public void MapLastToBuySellExchangeRates_MissingCurrencyName_ReturnsEmptyCurrencyName()
    {
        // Arrange
        // NBP answers before 2004-05-04 have no "currency" field.
        var source = new BuySellCurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();
        var dto = new BuySellCurrencyExchangeRatesDto { CurrencyCode = source.CurrencyCode, Rates = source.Rates };

        // Act
        var result = Mapper.MapLastToBuySellExchangeRates(dto);

        // Assert
        result.ShouldNotBeNull().CurrencyName.ShouldBeEmpty();
        result.CurrencyCode.ShouldBe(source.CurrencyCode);
    }
}
