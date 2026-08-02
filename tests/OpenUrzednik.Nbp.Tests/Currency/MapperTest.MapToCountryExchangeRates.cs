using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class MapperTest
{
    [Fact]
    public void MapToCountryExchangeRates_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var dto = new CountryExchangeRatesDtoFaker(0).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCountryExchangeRates(dto);

        // Assert
        result.ShouldBe(new CountryExchangeRates(dto.Country, dto.CurrencyName, dto.CurrencySymbol, dto.CurrencyCode, []));
    }

    [Fact]
    public void MapToCountryExchangeRates_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var dto = new CountryExchangeRatesDtoFaker(1).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCountryExchangeRates(dto);

        // Assert
        result.ShouldBe(new CountryExchangeRates(dto.Country, dto.CurrencyName, dto.CurrencySymbol, dto.CurrencyCode,
            [new ExchangeRate(dto.Rates[0].TableId, dto.Rates[0].PublicationDate, dto.Rates[0].Price)]));
    }

    [Fact]
    public void MapToCountryExchangeRates_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var dto = new CountryExchangeRatesDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCountryExchangeRates(dto);

        // Assert
        result.ShouldBe(new CountryExchangeRates(dto.Country, dto.CurrencyName, dto.CurrencySymbol, dto.CurrencyCode,
            [.. dto.Rates.Select(x => new ExchangeRate(x.TableId, x.PublicationDate, x.Price))]));
    }

    [Fact]
    public void MapToCountryExchangeRates_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CountryExchangeRatesDto dto = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToCountryExchangeRates(dto))
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToCountryExchangeRates_RatesArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var dtoBase = new CountryExchangeRatesDtoFaker(0).WithConstantSeed().Generate();
        var dto = new CountryExchangeRatesDto()
        {
            Country = dtoBase.Country,
            CurrencyName = dtoBase.CurrencyName,
            CurrencyCode = dtoBase.CurrencyCode,
            CurrencySymbol = dtoBase.CurrencySymbol,
            Rates = null!
        };
        
        // Act & Assert
        Should.Throw<ArgumentException>(() => Mapper.MapToCountryExchangeRates(dto))
            .ParamName.ShouldBe("dto");
    }
}