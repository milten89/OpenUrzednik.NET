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
    public void MapToCurrencyRate_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var dto = new CurrencyExchangeRateDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCurrencyRate(dto);

        // Assert
        result.ShouldBe(new CurrencyRate(dto.TableId, dto.PublicationDate, dto.Price));
    }

    [Fact]
    public void MapToCurrencyRate_NullDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        CurrencyExchangeRateDto dto = null!;

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToCurrencyRate(dto));
    }
}
