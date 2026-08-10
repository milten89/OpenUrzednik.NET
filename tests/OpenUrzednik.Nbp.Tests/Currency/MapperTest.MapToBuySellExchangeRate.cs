using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class MapperTest
{
    [Fact]
    public void MapToBuySellExchangeRate_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var dto = new BuySellCurrencyExchangeRateDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRate(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRate(dto.TableId, dto.PublicationDate, dto.Buy, dto.Sell));
    }

    [Fact]
    public void MapToBuySellExchangeRate_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        BuySellCurrencyExchangeRateDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToBuySellExchangeRate(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

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
            [new BuySellExchangeRate(dto.Rates[0].TableId, dto.Rates[0].PublicationDate, dto.Rates[0].Buy, dto.Rates[0].Sell)]));
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
            [.. dto.Rates.Select(x => new BuySellExchangeRate(x.TableId, x.PublicationDate, x.Buy, x.Sell))]));
    }

    [Fact]
    public void MapToBuySellExchangeRates_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        BuySellCurrencyExchangeRatesDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToBuySellExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToBuySellExchangeRates_RatesArrayContainingNullElement_ThrowsArgumentNullException()
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
        exception.ShouldBeOfType<ArgumentException>()
            .ParamName.ShouldBe("dto");
    }
}
