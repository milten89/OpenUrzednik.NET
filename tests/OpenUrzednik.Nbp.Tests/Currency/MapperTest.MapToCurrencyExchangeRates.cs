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
    public void MapToCurrencyExchangeRates_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var dto = new CurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName!, dto.CurrencyCode, []));
    }

    [Fact]
    public void MapToCurrencyExchangeRates_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var dto = new CurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName!, dto.CurrencyCode,
            [new CurrencyRate(dto.Rates[0].TableId, dto.Rates[0].PublicationDate.ToPublicDate(), dto.Rates[0].Price)]));
    }

    [Fact]
    public void MapToCurrencyExchangeRates_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var dto = new CurrencyExchangeRatesDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName!, dto.CurrencyCode,
            [.. dto.Rates.Select(x => new CurrencyRate(x.TableId, x.PublicationDate.ToPublicDate(), x.Price))]));
    }

    [Fact]
    public void MapToCurrencyExchangeRates_NullDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        CurrencyExchangeRatesDto dto = null!;

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToCurrencyExchangeRates(dto))
            .Message.ShouldBe("NBP API response is missing 'item'.");
    }

    [Fact]
    public void MapToCurrencyExchangeRates_RatesArrayContainingNullElement_ThrowsInvalidPayloadException()
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
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToCurrencyExchangeRates(dto))
            .Message.ShouldBe("NBP API response is missing 'rates'.");
    }

    [Fact]
    public void MapToCurrencyExchangeRates_MissingCurrencyName_ReturnsEmptyCurrencyName()
    {
        // Arrange
        // NBP answers before 2004-05-04 have no "currency" field.
        var source = new CurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();
        var dto = new CurrencyExchangeRatesDto { CurrencyCode = source.CurrencyCode, Rates = source.Rates };

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(string.Empty, source.CurrencyCode,
            [new CurrencyRate(source.Rates[0].TableId, source.Rates[0].PublicationDate.ToPublicDate(), source.Rates[0].Price)]));
    }
}
