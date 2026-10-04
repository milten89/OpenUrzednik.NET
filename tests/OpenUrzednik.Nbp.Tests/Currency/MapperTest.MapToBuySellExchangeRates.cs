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
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName, dto.CurrencyCode, []));
    }

    [Fact]
    public void MapToBuySellExchangeRates_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var dto = new BuySellCurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName, dto.CurrencyCode,
            [new CurrencyBuySellRate(dto.Rates[0].TableId, dto.Rates[0].PublicationDate, dto.Rates[0].Ask, dto.Rates[0].Bid)]));
    }

    [Fact]
    public void MapToBuySellExchangeRates_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var dto = new BuySellCurrencyExchangeRatesDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName, dto.CurrencyCode,
            [.. dto.Rates.Select(x => new CurrencyBuySellRate(x.TableId, x.PublicationDate, x.Ask, x.Bid))]));
    }

    [Fact]
    public void MapToBuySellExchangeRates_NullDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        BuySellCurrencyExchangeRatesDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToBuySellExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<InvalidPayloadException>();
    }

    [Fact]
    public void MapToBuySellExchangeRates_RatesArrayContainingNullElement_ThrowsInvalidPayloadException()
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
        exception.ShouldBeOfType<InvalidPayloadException>();
    }
}
