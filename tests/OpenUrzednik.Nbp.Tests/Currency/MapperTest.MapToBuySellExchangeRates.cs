using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class MapperTest
{
    [Fact]
    public void MapToBuySellExchangeRates_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var dto = new BuySellCurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName!, dto.CurrencyCode, []));
    }

    [Fact]
    public void MapToBuySellExchangeRates_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var dto = new BuySellCurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName!, dto.CurrencyCode,
            [new CurrencyBuySellRate(dto.Rates[0].TableId, dto.Rates[0].PublicationDate.ToPublicDate(), dto.Rates[0].Ask, dto.Rates[0].Bid)]));
    }

    [Fact]
    public void MapToBuySellExchangeRates_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var dto = new BuySellCurrencyExchangeRatesDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName!, dto.CurrencyCode,
            [.. dto.Rates.Select(x => new CurrencyBuySellRate(x.TableId, x.PublicationDate.ToPublicDate(), x.Ask, x.Bid))]));
    }

    [Fact]
    public void MapToBuySellExchangeRates_NullDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        BuySellCurrencyExchangeRatesDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToBuySellExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<InvalidPayloadException>().Message.ShouldBe("NBP API response is missing 'item'.");
    }

    [Fact]
    public void MapToBuySellExchangeRates_NullRates_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dtoBase = new BuySellCurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();
        var dto = new BuySellCurrencyExchangeRatesDto()
        {
            CurrencyName = dtoBase.CurrencyName,
            CurrencyCode = dtoBase.CurrencyCode,
            Rates = null!
        };

        // Act
        var exception = Record.Exception(() => Mapper.MapToBuySellExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<InvalidPayloadException>().Message.ShouldBe("NBP API response is missing 'rates'.");
    }

    [Fact]
    public void MapToBuySellExchangeRates_RatesContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dtoBase = new BuySellCurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();
        var dto = new BuySellCurrencyExchangeRatesDto()
        {
            CurrencyName = dtoBase.CurrencyName,
            CurrencyCode = dtoBase.CurrencyCode,
            Rates = [dtoBase.Rates[0], null!]
        };

        // Act
        var exception = Record.Exception(() => Mapper.MapToBuySellExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<InvalidPayloadException>().Message.ShouldBe("NBP API response is missing 'item'.");
    }

    [Fact]
    public void MapToBuySellExchangeRates_MissingCurrencyName_ReturnsEmptyCurrencyName()
    {
        // Arrange
        // NBP answers before 2004-05-04 have no "currency" field.
        var source = new BuySellCurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();
        var dto = new BuySellCurrencyExchangeRatesDto { CurrencyCode = source.CurrencyCode, Rates = source.Rates };

        // Act
        var result = Mapper.MapToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(string.Empty, source.CurrencyCode,
            [new CurrencyBuySellRate(source.Rates[0].TableId, source.Rates[0].PublicationDate.ToPublicDate(), source.Rates[0].Ask, source.Rates[0].Bid)]));
    }
}
