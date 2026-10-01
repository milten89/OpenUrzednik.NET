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
    public void MapToExchangeRate_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var dto = new ExchangeRateDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToExchangeRate(dto);

        // Assert
        result.ShouldBe(new ExchangeRate(dto.CurrencyName, dto.CurrencyCode, dto.Price));
    }

    [Fact]
    public void MapToExchangeRate_NullDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        ExchangeRateDto dto = null!;

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToExchangeRate(dto));
    }
}
