using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class MapperTest
{
    [Fact]
    public void MapToCurrencyExchangeRates_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var dto = new CurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName, dto.CurrencyCode, []));
    }

    [Fact]
    public void MapToCurrencyExchangeRates_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var dto = new CurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName, dto.CurrencyCode,
            [new ExchangeRate(dto.Rates[0].TableId, dto.Rates[0].PublicationDate, dto.Rates[0].Price)]));
    }

    [Fact]
    public void MapToCurrencyExchangeRates_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var dto = new CurrencyExchangeRatesDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName, dto.CurrencyCode,
            [.. dto.Rates.Select(x => new ExchangeRate(x.TableId, x.PublicationDate, x.Price))]));
    }

    [Fact]
    public void MapToCurrencyExchangeRates_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CurrencyExchangeRatesDto dto = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToCurrencyExchangeRates(dto))
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToCurrencyExchangeRates_RatesArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var dtoBase = new CurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();
        var dto = new CurrencyExchangeRatesDto
        {
            CurrencyName = dtoBase.CurrencyName,
            CurrencyCode = dtoBase.CurrencyCode,
            Rates = null!
        };

        // Act && Assert
        Should.Throw<ArgumentException>(() => Mapper.MapToCurrencyExchangeRates(dto))
            .ParamName.ShouldBe("dto");
    }
}
