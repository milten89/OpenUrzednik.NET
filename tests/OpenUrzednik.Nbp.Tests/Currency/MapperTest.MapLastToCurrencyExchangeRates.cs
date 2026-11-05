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
    public void MapLastToCurrencyExchangeRates_UnsortedRates_ReturnsOnlyRateWithLatestPublicationDate()
    {
        // Arrange
        var dto = new CurrencyExchangeRatesDtoFaker(5).WithConstantSeed().Generate();
        var expected = dto.Rates.OrderByDescending(x => x.PublicationDate).First();

        // Act
        var result = Mapper.MapLastToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName!, dto.CurrencyCode,
            [new CurrencyRate(expected.TableId, expected.PublicationDate.ToPublicDate(), expected.Price)]));
    }

    [Fact]
    public void MapLastToCurrencyExchangeRates_EmptyRates_ReturnsNull()
    {
        // Arrange
        var dto = new CurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapLastToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void MapLastToCurrencyExchangeRates_NullRates_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dtoBase = new CurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();
        var dto = new CurrencyExchangeRatesDto { CurrencyName = dtoBase.CurrencyName, CurrencyCode = dtoBase.CurrencyCode, Rates = null! };

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToCurrencyExchangeRates(dto));
    }

    [Fact]
    public void MapLastToCurrencyExchangeRates_RatesContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dtoBase = new CurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();
        var dto = new CurrencyExchangeRatesDto { CurrencyName = dtoBase.CurrencyName, CurrencyCode = dtoBase.CurrencyCode, Rates = [dtoBase.Rates[0], null!] };

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToCurrencyExchangeRates(dto));
    }

    [Fact]
    public void MapLastToCurrencyExchangeRates_MissingCurrencyName_ReturnsEmptyCurrencyName()
    {
        // Arrange
        // NBP answers before 2004-05-04 have no "currency" field.
        var source = new CurrencyExchangeRatesDtoFaker(1).WithConstantSeed().Generate();
        var dto = new CurrencyExchangeRatesDto { CurrencyCode = source.CurrencyCode, Rates = source.Rates };

        // Act
        var result = Mapper.MapLastToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldNotBeNull().CurrencyName.ShouldBeEmpty();
        result.CurrencyCode.ShouldBe(source.CurrencyCode);
    }
}
