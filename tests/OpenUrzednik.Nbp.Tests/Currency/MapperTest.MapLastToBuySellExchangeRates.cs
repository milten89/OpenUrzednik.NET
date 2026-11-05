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
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName, dto.CurrencyCode,
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
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToBuySellExchangeRates(null!));
    }

    [Fact]
    public void MapLastToBuySellExchangeRates_NullRates_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dtoBase = new BuySellCurrencyExchangeRatesDtoFaker(0).WithConstantSeed().Generate();
        var dto = new BuySellCurrencyExchangeRatesDto { CurrencyName = dtoBase.CurrencyName, CurrencyCode = dtoBase.CurrencyCode, Rates = null! };

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToBuySellExchangeRates(dto));
    }
}
