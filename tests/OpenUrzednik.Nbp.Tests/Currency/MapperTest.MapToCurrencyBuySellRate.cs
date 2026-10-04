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
    public void MapToCurrencyBuySellRate_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var dto = new BuySellCurrencyExchangeRateDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCurrencyBuySellRate(dto);

        // Assert
        result.ShouldBe(new CurrencyBuySellRate(dto.TableId, dto.PublicationDate, dto.Ask, dto.Bid));
    }

    [Fact]
    public void MapToCurrencyBuySellRate_NullDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        BuySellCurrencyExchangeRateDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToCurrencyBuySellRate(dto));

        // Assert
        exception.ShouldBeOfType<InvalidPayloadException>();
    }
}
