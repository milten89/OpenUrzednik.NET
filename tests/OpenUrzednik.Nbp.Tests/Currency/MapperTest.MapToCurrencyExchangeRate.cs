using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class MapperTest
{
    [Fact]
    public void MapToCurrencyExchangeRate_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var dto = new CurrencyExchangeRateDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToCurrencyExchangeRate(dto);

        // Assert
        result.ShouldBe(new ExchangeRate(dto.TableId, dto.PublicationDate, dto.Price));
    }

    [Fact]
    public void MapToCurrencyExchangeRate_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CurrencyExchangeRateDto dto = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToCurrencyExchangeRate(dto))
            .ParamName.ShouldBe("dto");
    }
}
