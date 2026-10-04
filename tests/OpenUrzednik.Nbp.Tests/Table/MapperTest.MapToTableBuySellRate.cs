using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class MapperTest
{
    [Fact]
    public void MapToTableBuySellRate_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var dto = new BuySellExchangeRateDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToTableBuySellRate(dto);

        // Assert
        result.ShouldBe(new TableBuySellRate(dto.CurrencyName, dto.CurrencyCode, dto.Ask, dto.Bid));
    }

    [Fact]
    public void MapToTableBuySellRate_NullDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        BuySellExchangeRateDto dto = null!;

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToTableBuySellRate(dto));
    }
}
